
using GunFightGame.Game;
using GunFightGame.Game.Player;

namespace GunFightGame._Project.Game;

public class GameData
{
    private GameState _gameState;

    public float KillToPlayerTime;
    public float ExtendTime=120f;
    public List<_Project.Player.Player?> Players;
    public  List<int> SendMessageIds;
    public int PlayersMessageId;
    public long TargetPartyId;
    public List<string> RandomStartGameGifs;
    public List<PlayersVs> PlayersVsList;
    public string GameToken;
    public long DebugId;

    public GameState GameState
    {
        get
        {
            return _gameState;
        }
        set
        {
            _gameState = value;
        }
    }

    public GameData ()
    {
        Random r = new Random();
        KillToPlayerTime =r.Next(45,60);
        ExtendTime = 120f;
        _gameState = GameState.Null;
    }
    public void GenerateGameToken()
    {
        string randomValue = Guid.NewGuid().ToString();
        GameToken =""+randomValue; 
    }

  
}