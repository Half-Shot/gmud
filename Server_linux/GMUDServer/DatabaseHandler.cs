using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Data;
using System.Threading;
using System.IO;
using System.Text;
using log4net;
using Newtonsoft.Json;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperWebSocket;
using Mono.Data.Sqlite;
namespace GMUDServer
{
    class DatabaseHandler
    {
        string dbConnection;
        public bool IntegrityOk = true;
		public SqliteConnection connection; 
        public DatabaseHandler(string dbfile)
        {
            if (!File.Exists(dbfile))
                IntegrityOk = CreateDB(dbfile);

            if (!IntegrityOk)
                return;

            dbConnection = String.Format("Data Source={0}", dbfile);
			connection = new SqliteConnection(dbConnection);
            connection.Open();
        }

        public static bool CreateDB(string dbfile)
        {
            SqliteConnection.CreateFile(dbfile);
            string dbConnection = String.Format("Data Source={0}", dbfile);
            SqliteConnection cnn = new SqliteConnection(dbConnection);
            cnn.Open();
            string[] commands = new string[5];

			
            //CreateTable
            SqliteCommand cmd = cnn.CreateCommand();
            int output = 0;
            try
            {
                cmd.CommandText = "CREATE TABLE accounts (username string primary key, password string, id integer)";
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating Table 'accounts' returned " + output), 2);

                cmd.CommandText = "CREATE TABLE players (ids integer primary key autoincrement, name text,level integer,attacklevel integer, attackxp integer,defencelevel integer, defencexp integer,magiclevel integer, magicxp integer, kills integer, health integer, admin integer, banned integer, inventory string)";
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating Table 'players' returned " + output), 2);
                
            }
            catch (SqliteException ex)
            {
                Logger.LogMsg("SQL> Failed last action, database not created. Destroying attempt. Exception data " + ex.Message, 1);
                File.Delete(dbfile);
                cnn.Close();
                return false;
                throw;
            }
            //CreateAdmin
            
            try
            {
				cmd = cnn.CreateCommand();
	            cmd.CommandText = "INSERT INTO players VALUES(0,'Administrator',1,1,0,1,0,1,0,0,10,1,0,'')";

                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new player 'Administrator' returned " + output), 2);

                cmd.CommandText = "INSERT INTO accounts VALUES('admin','admin',0)";
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new account 'admin' returned " + output), 2);
            }
            catch (SqliteException ex)
            {
                Logger.LogMsg("SQL> Failed last action, database not created. Destroying attempt. Exception data " + ex.Message, 1);
                cnn.Close();
                File.Delete(dbfile);
                return false;
                throw;
            }
            return true;


        }

        public string[] ListAllPlayers()
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM players";
            SqliteDataReader data = cmd.ExecuteReader();
            List<string> formatted_data = new List<string>();
            formatted_data.Add("ID   NAME  LEVEL ATT.L ATT.XP DEF.L DEF.XP MAG.L MAG.XP KILLS HEALTH ADMIN BANNED");
            while (data.Read())
	        {
                string str = " ";
	            for (int i = 0; i < data.FieldCount; i++)
			    {
			        str += data.GetValue(i) + "   ";
			    }
                formatted_data.Add(str);
	        }
            return formatted_data.ToArray();
        }

        public void ModifyPlayer()
        {

        }

        public void DeletePlayer()
        {

        }

        public bool AddPlayer(string name, int isadmin)
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO players VALUES(NULL,'" + name + "',1,1,0,1,0,1,0,0,10," + isadmin + ",0,'')";
            try
            {
                int output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new player '" + name + "' returned " + output), 3);
			}
            catch (SqliteException ex)
            {
                Logger.LogMsg("SQL> Failed last action. Details:" + ex.Message, 1);
                return false;
            }
            return true;
        }

		public bool CreateAccount (string name, string password, int id)
		{
			if (id < 0) {
				return false; //Invalid id.
			}
			SqliteCommand cmd = connection.CreateCommand();
			int output = 0;
			try {
				cmd.CommandText = "INSERT INTO accounts VALUES('"+ name +"','" + password +"',"+ id +")";
				output = cmd.ExecuteNonQuery();
				Logger.LogMsg ("SQL>Creating Account '" + name +"' returned " + output, 2);
			} catch (SqliteException ex) {
				Logger.LogMsg("SQL>Creating Account failed, Exception " + ex.Message, 2);
			}
			return MiscMethods.IntToBool(output);
		}

        public bool DoesPlayerExist(int id)
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ids FROM players";
            SqliteDataReader data = cmd.ExecuteReader();
            int[] ids = new int[data.Depth];
                return ids.Contains<int>(id);
               
        }

		public int GetPlayerID (string name)
		{
			SqliteCommand cmd = connection.CreateCommand ();
			cmd.CommandText = "SELECT * FROM players WHERE name = '" + name + "'";
			try {
				SqliteDataReader data = cmd.ExecuteReader ();
				data.Read ();
				object unfid = data.GetValue(0);
				if (unfid == null)
					return -1;
				else
					return int.Parse(unfid.ToString());
			} catch (SqliteException ex) {
				Logger.LogMsg("SQL>Couldn't lookup PlayerID from table. Exception " + ex.Message);
			}
			return -1;
		}

        public bool DoesPlayerExist(string name)
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM players";
            SqliteDataReader data = cmd.ExecuteReader();
            string[] names = new string[data.Depth];
            return names.Contains<string>(name);
        }

		public bool DoesAccountExist (string name)
		{
			SqliteCommand cmd = connection.CreateCommand ();
			cmd.CommandText = "SELECT * FROM accounts WHERE username = '" + name + "'";
			cmd.Prepare ();
			SqliteDataReader data = cmd.ExecuteReader ();
			if (data.GetValue (0).GetType() == typeof(DBNull)) {
				return false;
			}
			return true;
		}

        public Player LoadPlayer(int id)
        {
            SqliteCommand cmd;
            try
            {
                cmd = connection.CreateCommand();
            }
            catch (Exception)
            {
                return null;
            }
            
            cmd.CommandText = "SELECT * FROM players WHERE ids = " + id;
            SqliteDataReader data = cmd.ExecuteReader();
            object[] objs = new object[data.FieldCount];
            int i = 0;
            data.Read();
            while (i < data.FieldCount)
            {
                objs[i] = data.GetValue(i);
                i++;
            }
            Player player = new Player();

            //ID //FINISH
            try
            {
                player.id = int.Parse(objs[0].ToString());
                player.name = objs[1].ToString();
                player.level = int.Parse(objs[2].ToString());//Subway
                player.attacklevel = int.Parse(objs[3].ToString());
                player.attackxp = int.Parse(objs[4].ToString());
                player.defencelevel = int.Parse(objs[5].ToString());
                player.defencexp = int.Parse(objs[6].ToString());
                player.magiclevel = int.Parse(objs[7].ToString());
                player.magicxp = int.Parse(objs[8].ToString());
                player.kills = int.Parse(objs[9].ToString());
				player.health = int.Parse(objs[10].ToString());
                player.admin = MiscMethods.IntToBool(int.Parse(objs[11].ToString()));
                player.banned = MiscMethods.IntToBool(int.Parse(objs[12].ToString()));
                return player;
            }
            catch (Exception crap)
            {
                Logger.LogMsg("Loading player failed",1);
                Logger.LogMsg("Reason: " + crap.Message,2);
                return null;
            }

        }

        public int Authenticate(string[] userdata)
        {
            SqliteCommand cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM accounts WHERE username = '" + userdata[0] + "'";
            cmd.Prepare();
			SqliteDataReader data = cmd.ExecuteReader();
            object[] objs = new object[data.FieldCount];
            //if (data.Depth < 1)
            //    return -1;
            int i = 0;
            data.Read();
            while (i < data.FieldCount)
            {
                objs[i] = data.GetValue(i);
                i++;
            }

            if (objs[0] == null)
                return -1;

            if (objs[0].ToString() != userdata[0])
                return -2;

            if (objs[1].ToString() != userdata[1])
                return -3;

            return int.Parse(objs[2].ToString());
        }

    }
}
