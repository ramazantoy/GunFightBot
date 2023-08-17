using GunFightGame.Game;
using GunFightGame.Game.Player;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace GunFightGame._Project.Game;

public class GameMain
{
    public string GameToken
    {
        get
        {
            return _properties.GameToken; 
        }
   
    }

    public async Task<bool> IsAdmin(long userId)
    {
        try
        {
            ChatMember chatMember =await Program.BotClient!.GetChatMemberAsync(_properties.TargetPartyId, userId);

            if (chatMember.Status == ChatMemberStatus.Creator || chatMember.Status == ChatMemberStatus.Administrator) return true;

            return false;
        }
        catch (Exception e)
        {

            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.ToString());
            return false;
        }
        
    }

    public GameMain(long targetParyId)
    {
        _properties = new GameData();
        _properties.Players = new List<_Project.Player.Player?>();
        _properties.ExtendTime = 33f;
        _properties.TargetPartyId = targetParyId;
        _properties.DebugId = Program.DebugId;
        _properties.SendMessageIds = new List<int>();
        _properties.RandomStartGameGifs = new List<string>();
        _properties.PlayersVsList = new List<PlayersVs>();
        _properties.RandomStartGameGifs.Add("https://media.giphy.com/media/4H3fg9iWifJPLdDU9K/giphy.gif");
        _properties.RandomStartGameGifs.Add("https://media.giphy.com/media/nxXOJxnUW1wP4glihY/giphy.gif");
        _properties.RandomStartGameGifs.Add("https://media.giphy.com/media/Y0jao27TVBfNZndFlI/giphy.gif");
        _properties.RandomStartGameGifs.Add("https://media.giphy.com/media/Y0jao27TVBfNZndFlI/giphy.gif");
        _currentKillTimer = _properties.KillToPlayerTime;
    }

    private float _currentKillTimer = 0f;
    public GameData _properties;

    public long GroupId
    {
        get
        {
            return _properties.TargetPartyId;
        }
    }


    private async Task RestartBotState()
    {
        _properties.GameState = GameState.Null;
        _properties.Players = new List<_Project.Player.Player?>();

        _properties.ExtendTime = 33f;
        _properties.SendMessageIds = new List<int>();
        _properties.PlayersVsList = new List<PlayersVs>();
    }

    public async void Update(float deltaTime)
    {
        if (_properties.GameState == GameState.Null) return;

        if (_properties.GameState == GameState.WaitToPlayers)
        {
            _properties.ExtendTime -= deltaTime;
            if (_properties.ExtendTime >= 30 && _properties.ExtendTime % 30 == 0)
            {
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithUrl("Katıl", "https://t.me/gunfightbot?start=" + _properties.GameToken),
                    }
                });

                Message sendMessage = await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                    "Katılmak için " + _properties.ExtendTime + " saniye kaldı.", replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Html);
                _properties.SendMessageIds.Add(sendMessage.MessageId);
            }

            if (_properties.ExtendTime <= 0f)
            {
                await RunGame();
            }
        }
        else if (_properties.GameState == GameState.Playing)
        {
            _currentKillTimer -= deltaTime;
            
            if (!(_currentKillTimer <= 0f)) return;

            _currentKillTimer =_properties.KillToPlayerTime;
      
            
            await ShowVsDecide();
        }
    }

    public async Task StartGame(long userId)
    {
        if (_properties.GameState != GameState.Null)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                "Hali hazırda oynanılan bir oyun var.");
        }
        else
        {
            var user = Program.BotClient!.GetChatMemberAsync(chatId: _properties.TargetPartyId, userId: userId).Result
                .User;

            string userMention = $"<a href='tg://user?id={userId}'>{user.FirstName}</a> ";

            string message =
                "Düelloya hazır mısınız? Rakibinizi alt etmek için son şansınız! Kazananın yükselmesi için mücadele edin ve kazanan bir kişi siz olun! \n🏆Bol şanslar! 💪" +
                $"\nGun Fight {userMention} tarafından başlatıldı. \n  \n Katılmak için tıkla ! ";

            Random r = new Random();

            _properties.GenerateGameToken();
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithUrl("Katıl", "https://t.me/gunfightbot?start=" + _properties.GameToken),
                }
            });
            int randomIndex = r.Next(0, _properties.RandomStartGameGifs.Count);


            _properties.GameState = GameState.WaitToPlayers;

            Message sendMessage = await Program.BotClient!.SendAnimationAsync(
                chatId: _properties.TargetPartyId,
                animation: InputFile.FromUri(_properties.RandomStartGameGifs[randomIndex]),
                caption: message,
                replyMarkup: inlineKeyboard,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None);

            _properties.SendMessageIds.Add(sendMessage.MessageId);


            await FirstPlayerToList();

            await Program.BotClient!.PinChatMessageAsync(_properties.TargetPartyId, sendMessage.MessageId,
                disableNotification: true);


            await JoinTheGame(userId, user.FirstName + " " + user.LastName);
        }
    }

    public async Task ShowPlayersList()
    {
        if (_properties.GameState == GameState.Null)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                "Aktif oyun bulunamadı.\n/startgunfight yazarak düello başlatabilirsiniz.");
            return;
        }

        var playerCountText = "#players : " + _properties.Players.Count + "\n";

        var textMessage = playerCountText;
        var users = "";
        bool withState = _properties.GameState == GameState.Playing || _properties.GameState == GameState.End;
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            users += $"<a href='tg://user?id={player.UserId}'>{player.FirstName}</a> ";
            if (withState)
            {
                if (player.PlayerState == PlayerState.Alive)
                {
                    users += "😛";
                }
                else
                {
                    users += "☠️";
                }

                users += "\n";
            }
            else
            {
                users += "\n";
            }
        }

        textMessage += users;

        await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, "İşte son  liste");
        await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, textMessage,
            parseMode: ParseMode.Html);
    }

    private async Task ModifyFirstPlayersText()
    {
        var playerCountText = "#players : " + _properties.Players.Count + "\n";

        var textMessage = playerCountText;
        var users = "";
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            users += $"<a href='tg://user?id={player.UserId}'>{player.FirstName}</a> " + "\n";
        }

        textMessage += users;

        await Program.BotClient!.EditMessageTextAsync(_properties.TargetPartyId, _properties.PlayersMessageId,
            textMessage,
            ParseMode.Html);
    }

    private async Task FirstPlayerToList()
    {
        var playerCountText = "#players : " + _properties.Players.Count + "\n";

        var textMessage = playerCountText;
        var users = "";
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            users += $"<a href='tg://user?id={player.UserId}'>{player.FirstName}</a> " + "\n";
        }

        textMessage += users;

        Message message =
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, textMessage,
                parseMode: ParseMode.Html);
        _properties.PlayersMessageId = message.MessageId;
    }

    public async Task JoinTheGame(long userId, string firstName)
    {
        if (_properties.GameState != GameState.WaitToPlayers) return;

        if (IsContainsThePlayer(userId)) return;


        _Project.Player.Player? tempPlayer = new _Project.Player.Player(userId, firstName);

        _properties.Players.Add(tempPlayer);
        string userMention = $"<a href='tg://user?id={userId}'>{firstName}</a> " + " oyuna katıldı.";

        try
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Kaçış", "flee" + _properties.GameToken),
                }
            });
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, userMention,
                parseMode: ParseMode.Html);
            string chatName = Program.BotClient!.GetChatAsync(_properties.TargetPartyId).Result.Title!;

            var message = await Program.BotClient!.SendTextMessageAsync(userId,
                chatName + " Grubundaki Oyuna Başarıyla katıldınız.", replyMarkup: inlineKeyboard);
            tempPlayer.FleeMessageId = message.MessageId;
            await ModifyFirstPlayersText();
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }
    }

    private bool IsContainsThePlayer(long userId)
    {
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            if (player.UserId == userId) return true;
        }

        return false;
    }

    public async Task RunGame()
    {
        try
        {
            switch (_properties.GameState)
            {
                case GameState.Playing:
                    await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                        "Hali hazırda oynanılan bir oyun var.");
                    return;
                case GameState.Null:
                    await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                        "Aktif oyun bulunamadı.\n/startgunfight yazarak düello başlatabilirsiniz.");
                    return;
            }
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }


        if (_properties.GameState != GameState.WaitToPlayers) return;

        try
        {
            if (_properties.Players.Count < 2)
            {
                await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                    "Yeterli oyuncu bulunamadı oyun iptal ediliyor.");
                await DeleteGameStartMessages();
                await RestartBotState();
                await DeleteFleeButtons();

            }
            else
            {
                _properties.GameState = GameState.Playing;
                await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, "Oyun başlıyor. .");
                await DeleteGameStartMessages();
                await MatchPlayers();
            }
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }
    }

    private async Task DeleteFleeButtons()
    {
        foreach (_Project.Player.Player player in _properties.Players)
        {
            try
            {
                await Program.BotClient!.DeleteMessageAsync(player.UserId, player.FleeMessageId);
            }
            catch (Exception e)
            {
              await  Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.ToString());
            }
        }
    }

    private async Task DeleteGameStartMessages()
    {
        if (_properties.SendMessageIds.Count <= 0) return;

        InlineKeyboardMarkup? removeKeyboardMarkup = null;
        await Program.BotClient!.EditMessageReplyMarkupAsync(_properties.TargetPartyId, _properties.SendMessageIds[0],
            replyMarkup: removeKeyboardMarkup);

        for (var i = 1; i < _properties.SendMessageIds.Count; i++)
        {
            try
            {
                await Program.BotClient!.DeleteMessageAsync(_properties.TargetPartyId, _properties.SendMessageIds[i]);
            }
            catch (Exception e)
            {
                await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
            }
        }
        
        await DeleteFleeButtons();
    }

    public async Task ExtendGame(float time = 30)
    {
        try
        {
            switch (_properties.GameState)
            {
                case GameState.Playing:
                    Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                        "Hali hazırda oynanılan bir oyun var.");
                    return;
                case GameState.Null:
                    Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
                        "Aktif oyun bulunamadı.\n/startgunfight yazarak düello başlatabilirsiniz.");
                    return;
            }
        }
        catch (Exception e)
        {
            Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }

        _properties.ExtendTime += time;
        if (_properties.ExtendTime >= 300)
        {
            _properties.ExtendTime = 300;
        }

        Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId,
            "Oyunun süresi " + time + " saniye uzatıldı. Katılmak için " + _properties.ExtendTime + " saniye kaldı.");
    }

    private async Task MatchPlayers()
    {
        await RemoveDeathPlayers();

        _properties.PlayersVsList = new List<PlayersVs>();

        _Project.Player.Player? unmatchedPlayer = null;


        _properties.Players.ShuffleList();

        foreach (_Project.Player.Player? currentPlayer in _properties.Players)
        {
            if (currentPlayer!.PlayerState == PlayerState.Death || currentPlayer.PlayerState == PlayerState.Afk) continue;

            if (unmatchedPlayer == null)
            {
                unmatchedPlayer = currentPlayer;
            }
            else
            {
                currentPlayer.EnemyPlayer = unmatchedPlayer;
                unmatchedPlayer.EnemyPlayer = currentPlayer;
                await currentPlayer.ResetStatsForTour();
                await unmatchedPlayer.ResetStatsForTour();
                _properties.PlayersVsList.Add(new PlayersVs(unmatchedPlayer, currentPlayer));
                unmatchedPlayer = null;
            }
        }


        if (unmatchedPlayer != null)
        {
            _properties.PlayersVsList.Add(new PlayersVs(unmatchedPlayer, null));
        }

        if (HaveWinner())
        {
            EndGame(GetWinner());
            return;
        }

        await ShowVsPlayers();
        await SendDmMessages();
    }

    private bool HaveWinner()
    {
        int index = 0;
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            if (player.PlayerState == PlayerState.Alive)
            {
                index++;
            }
        }

        if (index <= 1)
        {
            return true;
        }

        return false;
    }

    private _Project.Player.Player? GetWinner()
    {
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            if (player.PlayerState == PlayerState.Alive) return player;
        }

        return null;
    }

    private async void EndGame(_Project.Player.Player? winnerPlayer)
    {
        if (winnerPlayer == null)
        {
            string noOneText = " Bir terslik oldu.\nEşleşen oyuncular afk kaldığı için kimse kazanamadı.";

            await Program.BotClient!.SendAnimationAsync(
                chatId: _properties.TargetPartyId,
                animation: InputFile.FromUri("https://media.giphy.com/media/3oriOdderbO8gZmi2s/giphy.gif"),
                caption: noOneText,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None);

            await RestartBotState();
            return;
        }

        string captionText =
            $"İşte gerçek bir silahşör! " +
            $"<a href='tg://user?id={winnerPlayer.UserId}'>{winnerPlayer.FirstName}</a>, " +
            $"diğer silahşörlerin tozunu attırdı ve kazanan olarak ortaya çıktı. " +
            $"Şimdi güneş batarken, adını bu arenada efsane olarak anacağız! 🌅🔫";
        ;


        try
        {
            await Program.BotClient!.SendAnimationAsync(
                chatId: _properties.TargetPartyId,
                animation: InputFile.FromUri("https://media.giphy.com/media/1YeKAMmdGI4dEUU8Wk/giphy.gif"),
                caption: captionText,
                parseMode: ParseMode.Html,
                cancellationToken: CancellationToken.None);
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }

        await RestartBotState();
    }

    private async Task ShowVsPlayers()
    {
        string vsText = "Yeni Eşleşmeler \n \n ";
        foreach (PlayersVs playersVs in _properties.PlayersVsList)
        {
            if (playersVs.Player2 == null)
            {
                vsText += $"<a href='tg://user?id={playersVs.Player1!.UserId}'>{playersVs.Player1.FirstName}</a> 😈" +
                          " üst tur'a çıktı.\n";
            }
            else
            {
                vsText += $"<a href='tg://user?id={playersVs.Player1!.UserId}'>{playersVs.Player1.FirstName}</a> 😈 " +
                          " vs " +
                          $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> 😈\n";
            }
        }

        await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, vsText, parseMode: ParseMode.Html);
    }

    private async Task RemoveDeathPlayers()
    {
        List<int> playersToRemoveIndexes = new List<int>();

        for (int i = 0; i < _properties.Players.Count; i++)
        {
            var player = _properties.Players[i];

            if (player.PlayerState == PlayerState.Afk || player.PlayerState == PlayerState.Death)
            {
                playersToRemoveIndexes.Add(i);
            }
        }


        for (int i = playersToRemoveIndexes.Count - 1; i >= 0; i--)
        {
            int indexToRemove = playersToRemoveIndexes[i];
            _properties.Players.RemoveAt(indexToRemove);
        }
    }

    private async Task SendDmMessages()
    {
        foreach (PlayersVs playersVs in _properties.PlayersVsList)
        {
            if (playersVs.Player2 != null)
            {
                var player1Keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(playersVs.Player1!.EnemyPlayer!.FirstName,"shoot"+_properties.GameToken),
                    }
                });
                var player2Keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(playersVs.Player2!.EnemyPlayer!.FirstName, "shoot"+_properties.GameToken),
                    }
                });

                try
                {
                    var player1Message = await Program.BotClient!.SendTextMessageAsync(playersVs.Player1.UserId,
                        "En son vuran kazanır.! \nAteş etmen için kalan süre "+_currentKillTimer+" saniye." ,
                        replyMarkup: player1Keyboard);
                    var player2Message = await Program.BotClient!.SendTextMessageAsync(playersVs.Player2.UserId,
                        "En son vuran kazanır.! \nAteş etmen için kalan süre "+_currentKillTimer+" saniye.", 
                        replyMarkup: player2Keyboard);

                    playersVs.Player1.DmMessageId = player1Message.MessageId;
                    playersVs.Player2.DmMessageId = player2Message.MessageId;
                }
                catch (Exception e)
                {
                    await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
                }
            }
            else
            {
                await Program.BotClient!.SendTextMessageAsync(playersVs.Player1!.UserId,
                    "Şanslısın üst tura yükseldin.");
            }
        }
    }

    public async Task ShootButtonDown(long playerId)
    {
        _Project.Player.Player? currentPlayer = await GetPlayer(playerId);

        if (currentPlayer == null || currentPlayer.PlayerState == PlayerState.Death ||
            currentPlayer.PlayerState == PlayerState.Afk) return;

        currentPlayer!.BulletTime = _properties.KillToPlayerTime;
        currentPlayer!.HaveShoot = true;

        try
        {
            if (currentPlayer.DmMessageId != -1)
            {
                await Program.BotClient!.DeleteMessageAsync(playerId, currentPlayer.DmMessageId);

                await Program.BotClient!.SendTextMessageAsync(playerId, "Ateş edildi.!💥");
            }
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.ToString());
        }
    }

    private Task<_Project.Player.Player?> GetPlayer(long playerId)
    {
        foreach (_Project.Player.Player? player in _properties.Players)
        {
            if (player!.UserId == playerId) return Task.FromResult(player);
        }

        return Task.FromResult<_Project.Player.Player>(null);
    }

    private async Task ShowVsDecide()
    {
        await RemoveButtons();
        var afkText = "";

        foreach (PlayersVs playersVs in _properties.PlayersVsList)
        {
            if (playersVs.Player2 == null) continue;


            if (playersVs.Player1!.HaveShoot == false)
            {
                playersVs.Player1.PlayerState = PlayerState.Afk;
                afkText +=
                    $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> afk kaldığı için öldü.\n";
            }

            if (playersVs.Player2!.HaveShoot == false)
            {
                playersVs.Player2.PlayerState = PlayerState.Afk;
                afkText +=
                    $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> afk kaldığı için öldü.\n";
            }

            if (playersVs.Player1.PlayerState != PlayerState.Afk && playersVs.Player2.PlayerState != PlayerState.Afk)
            {
                string captionText = "";
                if (playersVs.Player1!.BulletTime <= playersVs.Player2!.BulletTime)
                {
                    playersVs.Player2.PlayerState = PlayerState.Death;
                    captionText =
                        $"🔥🤺 Heyecan dorukta! " +
                        $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> " +
                        $"gururlu bir ifadeyle savaş alanında beklerken, " +
                        $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> " +
                        $"karşısında mağlubiyetle yere düşüyordu. 🥊";
                }
                else
                {
                    playersVs.Player1.PlayerState = PlayerState.Death;
                    captionText =
                        $"🔥🤺 Heyecan dorukta! " +
                        $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> " +
                        $"gururlu bir ifadeyle savaş alanında beklerken, " +
                        $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> " +
                        $"karşısında mağlubiyetle yere düşüyordu. 🥊";
                }

                try
                {
                    await Program.BotClient!.SendAnimationAsync(
                        chatId: _properties.TargetPartyId,
                        animation: InputFile.FromUri("https://media.giphy.com/media/5WIMcQNeu6TWoIrkfB/giphy.gif"),
                        caption: captionText,
                        parseMode: ParseMode.Html,
                        cancellationToken: CancellationToken.None);
                }
                catch (Exception e)
                {
                    await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
                }
            }
        }


        if (afkText != "")
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, afkText,
                parseMode: ParseMode.Html);
        }


        await ShotTourPlayers();
    }

    private async Task ShotTourPlayers()
    {
        string mentionText = "Düello Sonuçları \n \n";
        foreach (PlayersVs playersVs in _properties.PlayersVsList)
        {
            if (playersVs.Player2 == null) continue;

            string captionText = "";
            if (playersVs.Player1!.PlayerState == PlayerState.Afk)
            {
                captionText +=
                    $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> Afk\n";
            }

            if (playersVs.Player2!.PlayerState == PlayerState.Afk)
            {
                captionText +=
                    $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> Afk\n ";
            }

            if (playersVs.Player1!.HaveShoot || playersVs.Player2!.HaveShoot)
            {
                if (playersVs.Player1!.BulletTime <= playersVs.Player2!.BulletTime)
                {
                    captionText =
                        $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> 😛 vs " +
                        $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> ☠️\n";
                }
                else
                {
                    captionText =
                        $"<a href='tg://user?id={playersVs.Player1.UserId}'>{playersVs.Player1.FirstName}</a> ☠️ vs " +
                        $"<a href='tg://user?id={playersVs.Player2.UserId}'>{playersVs.Player2.FirstName}</a> 😛  \n";
                }
            }


            mentionText += captionText;
        }

        try
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, mentionText,
                parseMode: ParseMode.Html);
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.Message);
        }

        await MatchPlayers();
    }

    private Task RemoveButtons()
    {
        foreach (_Project.Player.Player player in _properties.Players)
        {
            player!.RemoveButton(_properties.TargetPartyId);
        }

        return Task.CompletedTask;
    }

    public async Task FleeButtonDown(long playerId)
    {
        _Project.Player.Player? playerTemp = await GetPlayer(playerId)!;
        if (playerTemp == null || _properties.GameState != GameState.WaitToPlayers) return;
   
        try
        {
            _properties.Players.Remove(playerTemp);
            
            await Program.BotClient!.DeleteMessageAsync(playerId, playerTemp.FleeMessageId);
            await Program.BotClient!.SendTextMessageAsync(playerId, "Oyundan ayrıldınız!.");
            
            string text = $"<a href='tg://user?id={playerTemp.UserId}'>{playerTemp.FirstName}</a>, çatışmanın kızgın gözlerinden kaçarak geri çekildi! 🏃‍♂️🔥 Ne yazık ki, vahşi batı arenasındaki düelloda cesaretini yitirerek oyundan ayrıldı. 💔";
            await Program.BotClient!.SendTextMessageAsync(_properties.TargetPartyId, text, parseMode: ParseMode.Html);
            await ModifyFirstPlayersText();
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.ToString());
        }
    }
}