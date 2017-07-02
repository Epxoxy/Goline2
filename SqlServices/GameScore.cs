using System;
using System.Text;

namespace SqlServices
{
    public class GameScore : ICloneable
    {
        public int ScoreID { get; set; }
        public int UserID { get; set; }
        public string NickName { get; set; }
        public int Score { get; set; }
        public int Win { get; set; }
        public int Fail { get; set; }
        public int Drawn { get; set; }
        public DateTime ModifyTime { get; set; }
        public int TotalTimes => Win + Fail + Drawn;

        internal string GetInsert(string tableName)
        {
            var builder = new StringBuilder();
            var valuesbuilder = new StringBuilder();
            builder.Append($"insert into {tableName} (");
            valuesbuilder.Append("values(");
            if (ScoreID != 0)
            {
                builder.Append("scoreid,");
                valuesbuilder.Append($"'{ScoreID}',");
            }
            if (UserID != 0)
            {
                builder.Append("userid,");
                valuesbuilder.Append($"'{UserID}',");
            }
            if (!string.IsNullOrEmpty(NickName))
            {
                builder.Append("nickname,");
                valuesbuilder.Append($"'{NickName}',");
            }
            if (Score != 0)
            {
                builder.Append("score,");
                valuesbuilder.Append($"'{Score}',");
            }
            if (Win != 0)
            {
                builder.Append("win,");
                valuesbuilder.Append($"'{Win}',");
            }
            if (Fail != 0)
            {
                builder.Append("fail,");
                valuesbuilder.Append($"'{Fail}',");
            }
            if (Drawn != 0)
            {
                builder.Append("drawn,");
                valuesbuilder.Append($"'{Drawn}',");
            }
            builder.Append($"modifytime) ");
            if (ModifyTime == default(DateTime)) ModifyTime = DateTime.Now;
            valuesbuilder.Append($"'{ModifyTime.ToString("yyyy-MM-dd HH:mm:ss")}')");
            builder.Append(valuesbuilder.ToString());
            return builder.ToString();
        }

        internal string GetUpdate(GameScore newScore, string tableName, string condition = "")
        {
            var builder = new StringBuilder();
            builder.Append($"update {tableName} set ");
            if (newScore.Score != this.Score) builder.Append($"score='{newScore.Score}',");
            if (newScore.Win != this.Win) builder.Append($"win='{newScore.Win}',");
            if (newScore.Fail != this.Fail) builder.Append($"fail='{newScore.Fail}',");
            if (newScore.Drawn != this.Drawn) builder.Append($"drawn='{newScore.Drawn}',");
            if (newScore.ModifyTime == default(DateTime)) newScore.ModifyTime = DateTime.Now;
            builder.Append($"modifytime='{newScore.ModifyTime.ToString("yyyy-MM-dd HH:mm:ss")}' ");
            builder.Append(string.IsNullOrEmpty(condition) ? $"where scoreid = '{ScoreID}'" : condition);
            return builder.ToString();
        }

        public object Clone()
        {
            return new GameScore()
            {
                ScoreID = this.ScoreID,
                UserID = this.UserID,
                NickName = this.NickName,
                Score = this.Score,
                Win = this.Win,
                Fail = this.Fail,
                Drawn = this.Drawn,
                ModifyTime = this.ModifyTime
            };
        }
    }
}
