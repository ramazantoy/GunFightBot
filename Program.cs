using GunFightGame._Project.Game;
using GunFightGame._Project.GroupManager;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using File = System.IO.File;

namespace GunFightGame;

class Program
{
    public static TelegramBotClient? BotClient;


    private static long _debugId;
    private static Timer? _timer;

    public static long DebugId
    {
        get
        {
            return _debugId;
        }
    }


    private static async Task Main()

    {
        Console.WriteLine("Gun Fight Beta v1.2 @leonbrave");
        
        string configPath = "config/config.txt";

        if (File.Exists(configPath))
        {
            string[] lines = await File.ReadAllLinesAsync(configPath);


            string? token = GetConfigValue(lines, "TELEGRAM_BOT_TOKEN");
            _debugId = long.Parse(GetConfigValue(lines, "DEBUG_GROUP_ID")!);

            if (!string.IsNullOrEmpty(token) || !string.IsNullOrEmpty(_debugId.ToString()))
            {
                BotClient = new TelegramBotClient(token);

                GroupManager.GenerateGroupData();

                using CancellationTokenSource cts = new();

                ReceiverOptions receiverOptions = new()
                {
                    AllowedUpdates = Array.Empty<UpdateType>()
                };

                BotClient.StartReceiving(
                    updateHandler: HandleUpdateAsync,
                    pollingErrorHandler: HandlePollingErrorAsync,
                    receiverOptions: receiverOptions,
                    cancellationToken: cts.Token
                );


                _timer = new Timer(TimerCallback, null, 0, 1000);

                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("Config.txt içerisindeki format uygun değil.");
            }
        }
        else
        {
            Console.WriteLine("Config.txt dosyası bulunamadı.");
        }
    }
    
    static string? GetConfigValue(string[] lines, string key)
    {
        foreach (string line in lines)
        {
            string?[] parts = line.Split('=');
            if (parts.Length == 2 && parts[0] == key)
            {
                return parts[1];
            }
        }

        return null;
    }

    private static void TimerCallback(object? state)
    {
        int intervalInMilliseconds = 1000;

        GroupManager.Update(intervalInMilliseconds / 1000f);
    }

    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
        CancellationToken cancellationToken)
    {
        if (update.CallbackQuery != null)
        {
            await ButtonDown(update);
        }

        if (update.Message is not { } message)
            return;

        if (message.Text is not { } messageText)
            return;

        String[] textParts = message.Text.Split(' ');

        string command = textParts[0];


        if (update.Message.Chat.Type == ChatType.Private)
        {
            try
            {
                if (command.Equals("/start"))
                {
                    if (textParts.Length <= 1 || !GroupManager.IsEqualAnyToken(textParts[1]))
                    {
                        await botClient.SendTextMessageAsync(message.From!.Id,
                            "Oyunu oynamak için grupta yer alan katıl butonuna tıklayarak oyuna gelmelisiniz.",
                            cancellationToken: cancellationToken);
                    }
                    else if (textParts.Length > 1)
                    {
                        try
                        {
                            GameMain? mainGame = GroupManager.GetMyGameWithToken(textParts[1]);
                            await mainGame!.JoinTheGame(message.From!.Id, message.From.FirstName);
                        }
                        catch (Exception e)
                        {
                            await Program.BotClient!.SendTextMessageAsync(_debugId, e.Message,
                                cancellationToken: cancellationToken);
                        }
                    }
                }
                else if (command.Equals("/help"))
                {
                    await botClient.SendTextMessageAsync(message.From!.Id,
                        "Merhaba! Ben, Silahşör Oyun Botu, heyecan dolu bir silahşör turnuvasının anahtar parçasıyım. Oyuncuları bir araya getirerek, ateşli çatışmaların ve stratejik hamlelerin hakim olduğu bu turnuvada eğlence dolu anlar yaşatıyorum." +
                        "\nNasıl Oynanır?" +
                        "\nOyunda amacınız, rakiplerinize karşı üstünlük sağlayarak son turun galibi olmaktır." +
                        "\nHer tur, oyuncuların sırayla ateş etme ve stratejik kararlar alma şansına sahip olduğu bir döngüyü içerir." +
                        "\nDoğru zamanlamayla ateş ederek rakiplerinizi elemeye çalışırken, kendinizi korumayı da unutmamalısınız." +
                        "\nSon turda ayakta kalan tek oyuncu zaferi kazanır!" +
                        "\nBotun yer aldığı bir grupta /startgunfight yazarak oyunu başlatabilirisinz.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception e)
            {
                await Program.BotClient!.SendTextMessageAsync(_debugId, e.Message,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            if (command == "/startgunfight" || command == "/startgunfight@gunfightbot")
            {
                GameMain gameMain = await GroupManager.GetMyGame(message.Chat.Id);
                await gameMain.StartGame(message.From!.Id);
            }
            else if (command == "/players" || command == "/players@gunfightbot")
            {
                GameMain gameMain = await GroupManager.GetMyGame(message.Chat.Id);
                await gameMain.ShowPlayersList();
            }
            else if (command == "/forcestart" || command == "/forcestart@gunfightbot")
            {
                GameMain gameMain = await GroupManager.GetMyGame(message.Chat.Id);
                bool isAdmin = await gameMain.IsAdmin(message.From!.Id);
                if (isAdmin)
                {
                    await gameMain.RunGame();
                }
                else
                {
                    await Program.BotClient!.SendTextMessageAsync(message.Chat.Id,
                        "Oyun yalnızca yöneticiler tarafından başlatılmaya zorlanabilir.");
                }
            }
            else if (command == "/extend" || command == "/extend@gunfightbot")
            {
                GameMain gameMain = await GroupManager.GetMyGame(message.Chat.Id);
                if (textParts.Length > 1)
                {
                    try
                    {
                        await gameMain.ExtendGame(float.Parse(textParts[1]));
                    }
                    catch (Exception e)
                    {
                        await gameMain.ExtendGame();
                        await Program.BotClient!.SendTextMessageAsync(_debugId, e.ToString(),
                            cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    await gameMain.ExtendGame();
                }
            }
        }
    }

    static Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };


        if (BotClient != null)
            BotClient.SendTextMessageAsync(_debugId, errorMessage, cancellationToken: cancellationToken);

        return Task.CompletedTask;
    }

    private static async Task ButtonDown(Update update)
    {
        if (update.CallbackQuery == null) return;

        string? data = update.CallbackQuery.Data;
        if (data != null && data.Length > 4)
        {
            string targetString = data.Substring(0, 4);
            if (targetString == "flee")
            {
                await GroupManager.FleeButtonDown(data, update.CallbackQuery.From.Id);
            }
            else if (targetString == "shoo")
            {
                await GroupManager.ShootButtonDown(data, update.CallbackQuery.From.Id);
            }
        }
    }
}