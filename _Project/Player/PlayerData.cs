namespace GunFightGame.Game.Player;

public class PlayerData
{
    public long UserId;
    public string FirstName;
    public PlayerState PlayerState;
    public _Project.Player.Player? EnemyPlayer;
    public float BulletTime;
    public int DmMessageId=-1;
    public bool HaveShoot;
    public int FleeMessageId=-1;
}