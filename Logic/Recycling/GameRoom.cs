using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogicUnit
{
    public delegate void MasterChangedEventHandler(int masterId);

    [Serializable]
    public class GameRoom
    {
        public event MasterChangedEventHandler MasterChanged;
        public int Id { get; set; }
        public string Name { get; set; }
        public int Limit { get; private set; }
        public bool IsRemote { get; set; }

        public int MasterId { get; private set; }

        private Dictionary<int, Player> players;
        private string masterToken;
        private int lastId = 0;
        private int idFrom = 100;

        private GameRoom(Player player, int limit)
        {
            player.Id = idFrom;
            this.Limit = limit;
            this.lastId = idFrom;
            this.players = new Dictionary<int, Player>(Limit);
        }

        public static GameRoom CreateRoom(Player player, int limit, out string token)
        {
            var room = new GameRoom(player, limit);
            room.masterToken = genToken();
            token = room.masterToken;
            return room;
        }

        public bool SignAsMaster(Player newMaster, string oldToken, ref string toToken)
        {
            if(oldToken == masterToken)
            {
                MasterId = newMaster.Id;
                masterToken = genToken();
                toToken = masterToken;
                MasterChanged?.Invoke(MasterId);
                return true;
            }
            return false;
        }

        public bool Join(Player player)
        {
            if (!players.ContainsValue(player))
            {
                player.Id = lastId + 1;
                players.Add(player.Id, player);
                return true;
            }
            return false;
        }

        public void Leave(int id)
        {
            if (players.ContainsKey(id))
            {
                players.Remove(id);
            }
        }

        private static string genToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        }

    }
}
