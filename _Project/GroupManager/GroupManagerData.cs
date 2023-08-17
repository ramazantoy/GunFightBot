using GunFightGame._Project.Game;

namespace GunFightGame._Project.GroupManager;

public class GroupManagerData
{
    public List<GameMain>? GroupList;
    
    private  string _usedGroupsIdsFile = "usedGroups.txt";

    public long DebugId = -1001794154143;

     public GroupManagerData()
     {
         GroupList = new List<GameMain>();
        CreateFileIfNeeded();
        LoadUsedGroups();
    }
    
    private  void LoadUsedGroups()
    {
        if (File.Exists(_usedGroupsIdsFile))
        {
            string[] lines = File.ReadAllLines(_usedGroupsIdsFile);
            foreach (string line in lines)
            {
                if (long.TryParse(line, out long groupId))
                {
                    GameMain gameMain = new GameMain(groupId);
                    GroupList.Add(gameMain);
                }
            }
        }
    }
    private void CreateFileIfNeeded()
    {
        if (!File.Exists(_usedGroupsIdsFile))
        {
            File.Create(_usedGroupsIdsFile).Close();
        }
    }
    
    public  void WriteNewGroupToFile(long chatId)
    {
        using (StreamWriter writer = File.AppendText(_usedGroupsIdsFile))
        {
            writer.WriteLine(chatId);
        }
    }
}