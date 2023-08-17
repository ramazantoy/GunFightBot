using GunFightGame.Game.Player;
using GunFightGame._Project.GroupManager;
using Telegram.Bot;

namespace GunFightGame._Project.Player;

public class Player
{
    private PlayerData _properties;

    public Player(long userId, string firstName)
    {
        _properties = new PlayerData();

        _properties.FirstName = firstName;
        _properties.UserId = userId;
        _properties.PlayerState = PlayerState.Alive;
        _properties.EnemyPlayer = null;
        _properties.BulletTime = 9999f;
        _properties.HaveShoot = false;
        _properties.FleeMessageId = -1;
        _properties.DmMessageId = -1;
    }

    public bool HaveShoot
    {
        get { return _properties.HaveShoot; }
        set { _properties.HaveShoot = value; }
    }

    public long UserId
    {
        get { return _properties.UserId; }
    }

    public string FirstName
    {
        get { return _properties.FirstName; }
    }

    public Player? EnemyPlayer
    {
        set { _properties.EnemyPlayer = value; }
        get { return _properties.EnemyPlayer; }
    }

    public float BulletTime
    {
        get { return _properties.BulletTime; }
        set { _properties.BulletTime = value; }
    }

    public int DmMessageId
    {
        set { _properties.DmMessageId = value; }
        get { return _properties.DmMessageId; }
    }

    public int FleeMessageId
    {
        set { _properties.FleeMessageId = value; }
        get { return _properties.FleeMessageId; }
    }

    public PlayerState PlayerState
    {
        set { _properties.PlayerState = value; }
        get { return _properties.PlayerState; }
    }

    public Task ResetStatsForTour()
    {
        _properties.HaveShoot = false;
        _properties.BulletTime = 9999f;
        return Task.CompletedTask;
    }

    public void RemoveButton(long chatId)
    {
        if (_properties.HaveShoot) return;

        try
        {
            Program.BotClient!.DeleteMessageAsync(_properties.UserId, _properties.DmMessageId);
            Program.BotClient!.SendTextMessageAsync(_properties.UserId, "Zaman Doldu!");
        }
        catch (Exception e)
        {
            Program.BotClient!.SendTextMessageAsync( GunFightGame._Project.GroupManager.GroupManager.DebugId, e.ToString());
        }
    }
}