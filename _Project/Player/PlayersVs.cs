namespace GunFightGame.Game.Player;

public class PlayersVs
{
    public _Project.Player.Player? Player1;
    public _Project.Player.Player? Player2;

    public PlayersVs(_Project.Player.Player? player1, _Project.Player.Player? player2)
    {
        Player1 = player1;
        Player2 = player2;
    }
}