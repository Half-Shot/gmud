using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Data;
using System.Threading;
using log4net;
using Newtonsoft.Json;
using SuperSocket.Common;
using SuperSocket.SocketBase;
using SuperSocket.SocketEngine;
using SuperWebSocket;
using Mono.Data.Sqlite;
namespace GMUDServer
{
    class Program
    {
        const string logfile = "server.log";
        const string databasefile = "database.sqlite";
        const int port = 2480;
        static Thread T_Server;
        public static MainServer server;
        public static DatabaseHandler database;
        static void Main(string[] args)
        {
			IPAddress IPUsed = IPAddress.Parse("0.0.0.0");
            Logger.OpenStream(logfile);
            server = new MainServer(IPUsed, port);
            Logger.loglevel = 3;
            Logger.LogMsg("Started Server - " + IPUsed.ToString() + ":" + port);
            Logger.LogMsg("Loading Database Handler.");
            database = new DatabaseHandler(Environment.CurrentDirectory + @"/" + databasefile);
            if(!DoChecks())
            {
                Logger.LogMsg("Server cannot continue due to a failed check. The Server will close in 10 seconds");
                Thread.Sleep(10000);
                System.Environment.Exit(0);
            }
            T_Server = new Thread( new ThreadStart(server.DoLogic));
            T_Server.Start();
            while (T_Server.IsAlive)
            {
                Console.Write(">");
                server.Command = Console.ReadLine();          
            }
            
        }

        static bool DoChecks()
        {
            bool Valid = true;
            string checkresult = "";

            //Logger working.
            FileInfo file = new FileInfo(logfile);
            if (file.Length > 0)
                checkresult = "OK";
            else
                checkresult = "WARNING";
            Logger.LogMsg("Logger Writing To File " + "[ " + checkresult + " ]",2);
            if (checkresult != "OK")
            {
                Logger.LogMsg("Problem not critical, continuing.", 2);
            }

            //Database file
            if (database.IntegrityOk)
                checkresult = "OK";
            else
            {
                checkresult = "CRITICAL";
                Valid = false;
            }

            Logger.LogMsg("Database: Integrity " + "[ " + checkresult + " ]", 2);

            Player player = database.LoadPlayer(0);
            if (player == null)
            {
                checkresult = "WARNING";
            }
            else
            {
                checkresult = "OK";
            }

            Logger.LogMsg("Player Loading " + "[ " + checkresult + " ]", 2);

            if (checkresult != "OK")
            {
                Logger.LogMsg("Problem not critical, continuing.", 2);
            }

            return Valid;

        }
    }

    class Logger
    {
        static FileStream stream;
        static string file = "";
        public static int loglevel = 1;
        public static void OpenStream(string filename)
        {
            file = filename;
            
        }

        public static void LogMsg (string msg, int level = 1)
		{
			if (loglevel < level) {
				return; //Ignore, we arn't checking.
			}

			while (stream == null) {
				stream = File.Open (file, FileMode.Append, FileAccess.Write);
			}

			 msg = "[" + DateTime.Now.ToShortTimeString () + "]" + msg + System.Environment.NewLine;
			 Console.WriteLine (msg);
			 byte[] bytes = ASCIIEncoding.ASCII.GetBytes (msg);
			 while (!stream.CanWrite) {
				try {
					stream = File.Open (file, FileMode.Append, FileAccess.Write);
				} catch (Exception) {

				}
				Thread.Sleep (50);
			 }
             stream.Write(bytes,0,bytes.Length);
             stream.Flush();
             LastMessage = msg;
             stream.Close();
        }

        public static string LastMessage = "";
    }

    class MainServer
    {
        WebSocketServer server;
		public Game MainGame;
        public List<Player> connected_players = new List<Player>();
        bool ShouldWork = true;
        public string Command = "";
        public MainServer(IPAddress address, int port)
        {
            server = new WebSocketServer();
            //Events
			server.Setup(2480);
			server.NewSessionConnected += new SessionHandler<WebSocketSession>(OnConnected);
			server.SessionClosed += new SessionHandler<WebSocketSession,CloseReason>(OnDisconnect);
			//server.NewRequestReceived += new RequestHandler<WebSocketSession,SuperWebSocket.Protocol.IWebSocketFragment>(OnConnect);
			server.NewMessageReceived += new SessionHandler<WebSocketSession,string>(OnMessage);
			server.Start();
			MainGame = new Game();
		}

        public void DoLogic()
        {
            while (ShouldWork)
            {
                //Commands
                if (Command != "")
                {
                    ExecuteCommand();
                    Command = "";
                }
                //Do game logic
				MainGame.entityHandler.Update();
                //Check for lost users
            }
            return;
        }
        

        public void OnConnect (WebSocketSession session,SuperWebSocket.Protocol.IWebSocketFragment frag)
		{
			Logger.LogMsg ("New User Connecting (" + session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port + ")");
		}

        public void OnConnected(WebSocketSession session)
        {
            Logger.LogMsg("User Connected (" + session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port + ")");
        }

        public void OnDisconnect (WebSocketSession session, CloseReason reason)
		{
			Player toRemove = null;
			string address = session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port;
			foreach (Player plr in connected_players) {
				if (plr.address == address) {
					toRemove = plr;
					break;
				}
			}

			if (toRemove != null) {
				connected_players.Remove (toRemove);
				Logger.LogMsg (toRemove.name + " disconnected from the server. Reason " + reason.ToString ());
			}
        }

        public void OnMessage(WebSocketSession session,string msg)
        {
			string address = session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port;
            Logger.LogMsg("Recieved from (" + address + ")" + "'" + msg + "'");
            PacketHandler.DeterminePacket(msg,session);
        }

        public void ExecuteCommand()
        {
			if(Command == null)
				return;
            string command = Command;
            string arguments = "";
            if (command.Contains(' '))
            {
                command = command.Substring(0, command.IndexOf(" ") );
                arguments = command.Substring(command.IndexOf(" ") + 1, command.Length - command.IndexOf(" ") - 1);
            }
            switch (command)
            {
                case "exit":
                    Logger.LogMsg("Closing Server : From Console");
                    System.Environment.Exit(0);
                    break;
                case "quit":
                    Logger.LogMsg("Closing Server : From Console");
                    System.Environment.Exit(0);
                    break;
                case "help":
                    Console.WriteLine("Commands avaliable:");
                    Console.WriteLine("Command      Function");
                    Console.WriteLine("exit,quit    Closes the server");
                    Console.WriteLine("sendraw     Sends a raw message to connected clients.");
                    Console.WriteLine("liststored   Lists stored players. Careful");
                    Console.WriteLine("removeplayer Remove a player from the server");
                    Console.WriteLine("ban          Bans a player");
                    Console.WriteLine("superme     Give a player admin rights.");
                    Console.WriteLine("setxp Set a players xp. (Attack0,Defence1,Magic2)");
                    Console.WriteLine("clear Clears the console");
                    break;
                case "say":
                    if (arguments == "")
                    {
                        Console.WriteLine("No arguments supplied.");
                        return;
                    }

                    Broadcast("[SAY]Server: " + arguments);
                    break;
                case "liststored":
                    foreach (string str in Program.database.ListAllPlayers())
                    {
                        Console.WriteLine(str);
                    }
                    break;
                case "clear":
                    Console.Clear();
                    break;
                default:
                    Console.WriteLine("Command not found.");
                    
                    break;
            }
        }

        public void Broadcast(string msg)
        {
            foreach (Player plr in connected_players)
            {
                plr.session.Send(msg);
            }
        }

        public void ParseClientCommand(string command, WebSocketSession session)
        {
            string cmd = command;
			string address = session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port;
			string arguments = "";
			Player plr =  GetPlayerByIP(address);
            if (cmd.Contains(" "))
            {
                cmd = command.Substring(0, command.IndexOf(" "));
                arguments = command.Substring(command.IndexOf(" ") + 1, command.Length - command.IndexOf(" ") - 1);
            }

            switch (cmd.ToLower())
            {
                case "say":
                    if (arguments == "")
                    {
                        Logger.LogMsg("Required arguments not found for command 'say'", 3);
                        return;
                    }
                    Broadcast("[SAY]" + plr.name + ":" + arguments);
                    break;
				case "move":
					switch(arguments)
					{
						case "left":
							plr.position.x--;
							break;
						case "right":
							plr.position.x++;
							break;
						case "up":
							plr.position.y--;
							break;
						case "down":
							plr.position.y++;
							break;
						default:
							//Can't move in that direction.
							break;
					}
					session.Send("[UPP]" + plr.ToString()); //Update player
					session.Send("[SAY] A mystic force tells you that you are at " + plr.position.x + "," + plr.position.y);
					break;
				case "help":
					session.Send("[SAY]Commands are:");
					session.Send("[SAY]say -> Chat to the world.");
					session.Send("[SAY]scan -> Find out whats around you.");
					session.Send("[SAY]move -> Move up, down, left or right.");
					session.Send("[SAY]attack -> Attempt a attack on a creature or player.");
					session.Send("[SAY]use -> Use an object.");
					break;
				case "scan":
					Entity[] ents =	MainGame.entityHandler.Scan(plr,25);
					if(ents.Count() < 1)
					session.Send("[SAY]You are all alone in the world...");
					foreach (Entity ent in ents) {
						session.Send("[SAY]You find a " + ent.name + " with a level of " + ent.level);
					}
					break;
				case "superscan":
					
				case "attack":
					break;
				case "use":
					break;
                default:
                    Logger.LogMsg("Player Command Unrecognised", 3);
                    session.Send("[SAY]Command Unrecognised");
                    break;
            }
        }

		public Player GetPlayerByIP(string ip)
		{	
			foreach (Player plr in connected_players){
				if(plr.address == ip)
					return plr;
			}
			return null;
		}
    }

    class DatabaseHandler
    {
        string dbConnection;
        public bool IntegrityOk = true;
		SqliteConnection connection; 
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
                cmd.Prepare();
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating Table 'accounts' returned " + output), 2);

                cmd.CommandText = "CREATE TABLE players (ids integer primary key, name text,level integer,attacklevel integer, attackxp integer,defencelevel integer, defencexp integer,magiclevel integer, magicxp integer, kills integer, health integer, admin integer, banned integer, inventory string)";
                cmd.Prepare();
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating Table 'players' returned " + output), 2);
                
            }
            catch (SqliteException)
            {
                Logger.LogMsg("SQL> Failed last action, database not created. Destroying attempt.", 1);
                File.Delete(dbfile);
                cnn.Close();
                return false;
                throw;
            }
            //CreateAdmin
            
            try
            {
				cmd = cnn.CreateCommand();
	            cmd.CommandText = "INSERT INTO players VALUE('Administrator',1,0,1,0,1,0,0,10,1,0,'')";

                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new player 'Administrator' returned " + output), 2);

                cmd.CommandText = "INSERT INTO accounts VALUES('admin','admin',0)";
                output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new account 'admin' returned " + output), 2);
            }
            catch (SqliteException)
            {
                Logger.LogMsg("SQL> Failed last action, database not created. Destroying attempt.", 1);
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
            cmd.CommandText = "INSERT INTO players VALUES(" + name + ",1,0,1,0,1,0,0,10," + isadmin + ",0,'')";
            try
            {
                int output = cmd.ExecuteNonQuery();
                Logger.LogMsg(("SQL>Creating new player '" + name + "' returned " + output), 3);
			}
            catch (SqliteException)
            {
                Logger.LogMsg("SQL> Failed last action.", 1);
                return false;
                throw;
            }
            return true;
        }

		public bool CreateAccount (string name, string password, int id)
		{
			SqliteCommand cmd = new SqliteCommand ();
			int output = 0;
			try {
				cmd.CommandText = "INSERT INTO accounts VALUES('"+ name +"','" + password +"',"+ id +")";
				cmd.Prepare ();
				output = cmd.ExecuteNonQuery ();
				Logger.LogMsg (("SQL>Creating Account '" + name +"' returned " + output), 2);
			} catch (SqliteException) {

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
			cmd.CommandText = "SELECT * FROM players WHERE name = " + name;
			SqliteDataReader data = cmd.ExecuteReader ();
			data.Read ();
			object unfid = data.GetValue(0);
			if (unfid == null)
				return -1;
			else
				return int.Parse(unfid.ToString());

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

    class MiscMethods
    {
		public static string StringArrayToString (string[] array)
		{
			string output = "";
			foreach (string line in array) {
				output += line + ",";
			}
			output.Remove(output.Length - 1);
			return output;
		}

        public static bool IntToBool(int input)
        {
            if (input > 0)
                return true;
            else
                return false;
        }

        public static int BoolToInt(bool input)
        {
            if (input)
                return 1;
            else
                return 0;
        }
    }

    class PacketHandler
    {
        public static void DeterminePacket(string msg, WebSocketSession uc)
        {
            //[TPE]
            string determiner = msg.Substring(1, 3);
            string arguments = msg.Substring(5, msg.Length - 5);
			string[] argarray = arguments.Split(',');
            switch (determiner)
            {
                case "RET":
                    Logger.LogMsg("Ping Returned.", 3);
                    break;
                case "REQ":
                    Logger.LogMsg("Ping Requested", 3);
                    uc.Send("[RET]PING!!!");
                    break;
                case "LOD":
                    Logger.LogMsg("Load User Request.", 3);
                    Player newplayer = Program.database.LoadPlayer(int.Parse(arguments));
					newplayer.session = uc;
					newplayer.address = uc.RemoteEndPoint.Address.ToString() + ":" + uc.RemoteEndPoint.Port;
                    uc.Send("[LOD]" + newplayer.ToString());
                    Program.server.connected_players.Add(newplayer);
                    break;
                case "CMD":
                    Logger.LogMsg("Command recieved : " + arguments, 3);
                    Program.server.ParseClientCommand(arguments, uc);
                    break;
                case "ACN":
                    Logger.LogMsg("User requires account details, checking authentication.",3);
                    int response = Program.database.Authenticate(arguments.Split(','));
                    switch (response)
	                {
                        case -1:
                            uc.Send("[ACN]002"); //NotFound
                            break;
                        case -2:
                            uc.Send("[ACN]002"); //NotFound
                            break;
                        case -3:
                            uc.Send("[ACN]003"); //PasswordWrong
                            break;
		                default:
                            uc.Send("[ACN]" + response); //Retrieved ok
                            break;
	                }
                    break;
				case "NEW":
					Logger.LogMsg("New user request.");
					//Usr,Pwd,Acc
					string problems = "";
					if(argarray[0] == "" || argarray[0].Length > 16)
						problems += "2";
					
					if(argarray[1] == "" || argarray[1].Length > 16)
						if(problems.Length > 0)
							problems += ",";
						problems += "3";	

					if(argarray[2] == "" || argarray[2].Length > 16)
						if(problems.Length > 0)
							problems += ",";
						problems += "4";	
					
					if(Program.database.DoesAccountExist(argarray[0]))
					{
						if(problems.Length > 0)
							problems += ",";
						problems += "1";	
					}
					if(problems == ""){
						uc.Send("[NEW]0");
						Program.database.AddPlayer(argarray[2],0);
						Program.database.CreateAccount(argarray[0],argarray[1],Program.database.GetPlayerID(argarray[2]));
					}
					else{
					uc.Send("[NEW]" + problems);}
					break;
				case "MAP":
					//Request for map data.
					string mapdata = MiscMethods.StringArrayToString(Program.server.MainGame.map.ToStringArray());
					uc.Send("[MAP]" + mapdata);
					break;
                default:
                    break;
            }
        }
    }
}
