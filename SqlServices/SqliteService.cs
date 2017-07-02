using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SqlServices
{
    public enum ConnectResult
    {
        Success,
        ConnectFail,
        EmptyUserNameOrPsw,
        UserExist,
        UserNotExist,
        WrongPassword,
        GenerateRecordFail,
        OtherError
    }
    public class SqliteService
    {
        string connectionString = Properties.Settings.Default.SqliteConnectString;
        public bool HasConnected { get; private set; }
        public OnlineAccount LoginedAccount { get; private set; }

        public SqliteService()
        {
            EnsureTableCreated();
        }

        private static SqliteService sqliteService;
        private static object lockhelper = new object();
        public static SqliteService GetService()
        {
            lock (lockhelper)
            {
                if (sqliteService == null) sqliteService = new SqliteService();
                return sqliteService;
            }
        }

        internal SQLiteConnection GetConnection()
        {
            SQLiteConnection connection = new SQLiteConnection(connectionString);
            //connection.SetPassword(Properties.Resources.DBPsw);
            //connection.Open();
            //connection.ChangePassword(Properties.Settings.Default.SqlitePsw);
            return connection;
        }

        public void EnsureTableCreated()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string sqlstring = "select count(*) as `count` from Sqlite_master where type = 'table' and name = 'scores';";
                var command = new SQLiteCommand(sqlstring, connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int count = Convert.ToInt32(reader["count"]);
                    if (count > 0) return;
                }
                //SQLiteConnection.CreateFile("GameScore.sqlite");
                var createTableCmd = new SQLiteCommand("create table scores (id INTEGER PRIMARY KEY AUTOINCREMENT, name varchar(20), score int, modifytime datetime DEFAULT (datetime(CURRENT_TIMESTAMP,'localtime')))", connection);
                createTableCmd.ExecuteNonQuery();
            }
        }

        public string UpdateScore(int scoreid, int addScore, GameResult result)
        {
            if (result == GameResult.Ended) return string.Empty;
            GameScore newscore = null;
            GameScore oldscore = null;
            newscore = GetScoreOfUser(scoreid);
            bool isNewRecord = false;
            if (newscore == null)
            {
                newscore = new GameScore();
                newscore.ScoreID = scoreid;
                isNewRecord = true;
            }
            else oldscore = (GameScore)newscore.Clone();
            newscore.ModifyTime = DateTime.Now;
            //Modify data
            newscore.Score += addScore;
            switch (result)
            {
                case GameResult.Drawn: ++newscore.Drawn; break;
                case GameResult.Win: ++newscore.Win; break;
                case GameResult.Fail: ++newscore.Fail; break;
                default: return string.Empty;
            }
            //Update to database
            string cmdString = isNewRecord ? newscore.GetInsert("scores") : oldscore.GetUpdate(newscore, "scores");
            System.Diagnostics.Debug.WriteLine(cmdString);
            using (var connection = GetConnection())
            {
                connection.Open();
                var cmd = new SQLiteCommand(cmdString, connection);
                System.Diagnostics.Debug.WriteLine($"Update local score : {cmd.ExecuteNonQuery()}");
            }
            return cmdString;
        }

        public GameScore QueryScore(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return null;
            string cmdString = $"select * from scores {condition};";
            using (var connection = GetConnection())
            {
                connection.Open();
                var insertcmd = new SQLiteCommand(cmdString, connection);
                var reader = insertcmd.ExecuteReader();
                GameScore score = null;
                while (reader.Read())
                {
                    score = new GameScore()
                    {
                        ScoreID = Convert.ToInt32(reader["scoreid"]),
                        UserID = Convert.ToInt32(reader["userid"]),
                        NickName = reader["nickname"].ToString(),
                        Score = Convert.ToInt32(reader["score"]),
                        Win = Convert.ToInt32(reader["win"]),
                        Fail = Convert.ToInt32(reader["fail"]),
                        Drawn = Convert.ToInt32(reader["drawn"]),
                        ModifyTime = Convert.ToDateTime(reader["modifytime"])
                    };
                }
                return score;
            }
        }

        internal int ExecuteNonQuery(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString)) return 0;
            int result;
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand(cmdString, connection);
                result = command.ExecuteNonQuery();
            }
            return result;
        }

        internal object ExecuteScalar(string cmdString)
        {
            if (string.IsNullOrEmpty(cmdString)) return 0;
            object result;
            using (var connection = GetConnection())
            {
                connection.Open();
                var command = new SQLiteCommand(cmdString, connection);
                result = command.ExecuteScalar();
            }
            return result;
        }

        public IList<GameScore> GetAllScore()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                var insertcmd = new SQLiteCommand("select * from scores", connection);
                var reader = insertcmd.ExecuteReader();
                List<GameScore> gamescores = new List<GameScore>();
                while (reader.Read())
                {
                    gamescores.Add(new GameScore()
                    {
                        ScoreID = Convert.ToInt32(reader["scoreid"]),
                        UserID = Convert.ToInt32(reader["userid"]),
                        NickName = reader["nickname"].ToString(),
                        Score = Convert.ToInt32(reader["score"]),
                        Win = Convert.ToInt32(reader["win"]),
                        Fail = Convert.ToInt32(reader["fail"]),
                        Drawn = Convert.ToInt32(reader["drawn"]),
                        ModifyTime = Convert.ToDateTime(reader["modifytime"])
                    });
                }
                return gamescores;
            }
        }

        public GameScore GetScoreOfUser(int scoreid)
        {
            return QueryScore($"where scoreid ='{scoreid}'");
        }

        public GameScore TrySync()
        {
            return QueryScore($"where userid = '{LoginedAccount.UserID}'");
        }

        /// <summary>
        /// Generate hash string with salt
        /// </summary>
        /// <param name="password">password</param>
        /// <param name="salt">salt</param>
        /// <returns></returns>
        private string GenerateHashString(string password, string salt)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(salt)) return string.Empty;
            byte[] passwordAndSaltBytes = System.Text.Encoding.UTF8.GetBytes(password + salt);
            byte[] hashBytes = new System.Security.Cryptography.SHA256Managed().ComputeHash(passwordAndSaltBytes);
            return Convert.ToBase64String(hashBytes);
        }

        /// <summary>
        /// Register a new account on online server
        /// </summary>
        /// <param name="name">Show name</param>
        /// <param name="username">Login name</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public ConnectResult Register(string nickName, string userName, string password, out string errorMsg)
        {
            errorMsg = string.Empty;
            Logout();
            try
            {
                using (SQLiteConnection conn = GetConnection())
                {
                    string selectString = $"select count(*) as count from users where username = '{userName}';";
                    var command = new SQLiteCommand(selectString, conn);
                    conn.Open();
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        int count = Convert.ToInt32(reader["count"]);
                        if (count > 0)
                        {
                            errorMsg = "Account exists!";
                            return ConnectResult.UserExist;
                        }
                    }
                    reader.Close();
                    //Generate hash string and salt
                    string salt = Guid.NewGuid().ToString();
                    string hashString = GenerateHashString(password, salt);
                    //Execute query
                    string sqlString = $"insert into users (nickname, username, password, salt) values('{nickName}', '{userName}', '{hashString}', '{salt}')";
                    SQLiteCommand cmd = new SQLiteCommand(sqlString, conn);
                    int result = cmd.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine($"Result effect is {result}");
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"StackTrace:\n {e.StackTrace} \nMessage:\n{e.Message}");
                errorMsg = e.Message;
                return ConnectResult.OtherError;
            }
            return Login(userName, password, out errorMsg);
        }

        /// <summary>
        /// Login to online server
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="password">Password</param>
        /// <returns></returns>
        public ConnectResult Login(string userName, string password, out string errorMsg)
        {
            //Check data
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(userName))
            {
                errorMsg = "Empty userName/password!";
                return ConnectResult.EmptyUserNameOrPsw;
            }
            HasConnected = false;
            LoginedAccount = null;
            OnlineAccount account = null;
            GameScore score = null;
            errorMsg = string.Empty;
            try
            {
                //Get salt and verify if user exist.
                string salt = string.Empty;
                using (SQLiteConnection conn = GetConnection())
                {
                    SQLiteCommand cmd = new SQLiteCommand($"select * from users where username = '{userName}'", conn);
                    conn.Open();
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        salt = Convert.ToString(reader["salt"]);
                    }
                }
                if (string.IsNullOrEmpty(salt))
                {
                    errorMsg = "User not exist!";
                    return ConnectResult.UserNotExist;
                }
                //Verify password
                string hashString = GenerateHashString(password, salt);
                ExecuteReader($"select * from users where username = '{userName}' and password = '{hashString}';", reader =>
                {
                    while (reader.Read())
                    {
                        account = new OnlineAccount();
                        account.State = AccountState.Online;
                        account.UserName = userName;
                        account.UserID = Convert.ToInt32(reader["id"]);
                        account.NickName = reader["nickname"].ToString();
                    }
                });
                //Check if login successed
                if (account == null)
                {
                    errorMsg = "Wrong password";
                    return ConnectResult.WrongPassword;
                }
                //Check record in scores
                ExecuteReader($"select * from scores where userid = '{account.UserID}';", reader =>
                {
                    while (reader.Read())
                    {
                        score = new GameScore()
                        {
                            ScoreID = Convert.ToInt32(reader["scoreid"]),
                            UserID = account.UserID,
                            NickName = Convert.ToString(reader["nickname"]),
                            Score = Convert.ToInt32(reader["score"]),
                            Win = Convert.ToInt32(reader["win"]),
                            Fail = Convert.ToInt32(reader["fail"]),
                            Drawn = Convert.ToInt32(reader["drawn"]),
                            ModifyTime = Convert.ToDateTime(reader["modifytime"])
                        };
                    }
                });
                //If record zero, create it
                if (score == null)
                {
                    System.Diagnostics.Debug.WriteLine($"score is null");
                    var newscore = new GameScore()
                    {
                        UserID = account.UserID,
                        NickName = account.NickName,
                        ModifyTime = DateTime.Now
                    };
                    if (ExecuteNonQuery(newscore.GetInsert("scores")) > 0)
                    {
                        score = newscore;
                        System.Diagnostics.Debug.WriteLine($"Insert record successed");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Insert record to scores fail");
                        errorMsg = "Insert record to scores fail";
                        return ConnectResult.GenerateRecordFail;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine($"StackTrace:\n {e.StackTrace} \nMessage:\n{e.Message}");
                errorMsg = e.Message;
                return ConnectResult.OtherError;
            }
            if (score != null)
            {
                LoginedAccount = account;
                LoginedAccount.Score = score;
                return ConnectResult.Success;
            }
            return ConnectResult.OtherError;
        }

        /// <summary>
        /// Release Account
        /// </summary>
        public void Logout()
        {
            LoginedAccount = null;
            HasConnected = false;
        }

        public IList<GameScore> GetTop(int num)
        {
            List<GameScore> gamescores = new List<GameScore>();
            try
            {
                ExecuteReader($"select top {num}*  from scores order by score desc; ", reader =>
                {
                    while (reader.Read())
                    {
                        gamescores.Add(new GameScore()
                        {
                            ScoreID = Convert.ToInt32(reader["scoreid"]),
                            UserID = Convert.ToInt32(reader["userid"]),
                            NickName = reader["nickname"].ToString(),
                            Score = Convert.ToInt32(reader["score"]),
                            Win = Convert.ToInt32(reader["win"]),
                            Fail = Convert.ToInt32(reader["fail"]),
                            Drawn = Convert.ToInt32(reader["drawn"]),
                            ModifyTime = Convert.ToDateTime(reader["modifytime"])
                        });
                    }
                });
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.StackTrace);
            }
            return gamescores;
        }

        internal void ExecuteReader(string cmdString, Action<SQLiteDataReader> action)
        {
            if (string.IsNullOrEmpty(cmdString)) return;
            System.Diagnostics.Debug.WriteLine(cmdString);
            using (SQLiteConnection conn = GetConnection())
            {
                SQLiteCommand cmd = new SQLiteCommand(cmdString, conn);
                conn.Open();
                var reader = cmd.ExecuteReader();
                action.Invoke(reader);
            }
        }


    }
}
