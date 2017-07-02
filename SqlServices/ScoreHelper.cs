using System.Text;

namespace SqlServices
{
    public class ScoreHelper
    {
        #region --------Sql operate--------

        private void UpdateScore(int[] ids, int winnerId, bool newWinner)
        {
            if (ids != null && ids.Length > 0)
            {
                int winscore = 1, drawnscore = 0, failscore = -1;
                var builder = new StringBuilder();
                var sqliteService = SqliteService.GetService();
                foreach (var id in ids)
                {
                    string cmdString = string.Empty;
                    if (!newWinner)
                    {
                        sqliteService.UpdateScore(id, drawnscore, GameResult.Drawn);
                    }
                    else if (id == winnerId)
                    {
                        sqliteService.UpdateScore(id, winscore, GameResult.Win);
                    }
                    else
                    {
                        sqliteService.UpdateScore(id, failscore, GameResult.Fail);
                    }
                }
                /* Use online database version
                var sqlserverService = RemoteSqlServerService.GetService();
                if (sqlserverService.HasConnected)
                    sqlserverService.TrySync();*/
            }
        }

        #endregion
    }
}
