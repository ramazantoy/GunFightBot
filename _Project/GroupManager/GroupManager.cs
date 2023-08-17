
using GunFightGame._Project.Game;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace GunFightGame._Project.GroupManager;


public static class GroupManager
{
    private static GroupManagerData _properties;

    public static void GenerateGroupData()
    {
        _properties = new GroupManagerData();
    }

    public static ChatId DebugId
    {
        get
        {
            return _properties.DebugId;
        }
    }
    public static async Task<GameMain> GetGame(long groupId)
    {
        foreach (GameMain gameMain in _properties.GroupList)
        {
            if (gameMain.GroupId == groupId) return gameMain;
        }

        GameMain gameMainTemp = new GameMain(groupId);
        
        _properties.GroupList.Add(gameMainTemp);
        _properties.WriteNewGroupToFile(groupId);

        try
        {
            var chat =await Program.BotClient!.GetChatAsync(groupId);
            var count = await Program.BotClient!.GetChatMemberCountAsync(groupId);
            
         await   Program.BotClient!.SendTextMessageAsync(_properties.DebugId, "Bot yeni bir gruba eklendi");

         await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, "Grup Bilgileri \n Grup ismi : " + chat.Title + "\n Üye sayısı :" + count + "\n Chat Id : [" + groupId + "]");
        }
        catch (Exception e)
        {
            await Program.BotClient!.SendTextMessageAsync(_properties.DebugId, e.ToString());
        }
        return gameMainTemp;
    }

    public static void Update(float time)
    {
        if(_properties.GroupList!.Count<=0) return;
        
        foreach (GameMain gameMain in _properties.GroupList)
        {
            gameMain.Update(time);
        }
    }

    public static bool IsEqualAnyToken(string token)
    {
        return _properties.GroupList!.Any(gameMain => gameMain.GameToken == token);
    }

    public  static GameMain? GetMyGameWithToken(string token)
    {
        return _properties.GroupList!.FirstOrDefault(gameMain => gameMain.GameToken == token);
    }

    public static Task<GameMain> GetMyGame(long chatId)
    {
        return GetGame(chatId);
    }

    public static async Task FleeButtonDown(string fleeToken,long userId)
    {
        string flee = "flee";
        foreach (GameMain gameMain in _properties.GroupList!)
        {
            if (flee + gameMain.GameToken == fleeToken)
            {
             await   gameMain.FleeButtonDown(userId);
             return;
            }
        }
    }

    public static async Task ShootButtonDown(string gameToken,long userId)
    {
        gameToken=gameToken.Substring(5);
        GameMain? gameMain =GetMyGameWithToken(gameToken);
        
        if(gameMain==null) return;
        
        await gameMain.ShootButtonDown(userId);
    }

   
}