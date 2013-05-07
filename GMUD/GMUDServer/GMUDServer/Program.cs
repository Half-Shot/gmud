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
		static Thread T_Logger;
        public static MainServer server;
        public static DatabaseHandler database;
        static void Main (string[] args)
		{
			IPAddress IPUsed = IPAddress.Parse ("0.0.0.0"); //Set the right ip address.

			//Logger setup.
			Logger.file = Environment.CurrentDirectory + "/" + logfile; //Set up the correct file.
			Logger.loglevel = 3; //Sets the level of verboseness
			T_Logger = new Thread (Logger.DoLogger); //A thread to check through the buffer
			T_Logger.Start ();
			while (!T_Logger.IsAlive) {
				System.Diagnostics.Debug.WriteLine("Logger Thread not ready!");
				Thread.Sleep(25);
			}

			server = new MainServer (IPUsed, port);
			Logger.LogMsg ("Started Server - " + IPUsed.ToString () + ":" + port); //Update the user.
			database = new DatabaseHandler (Environment.CurrentDirectory + @"/" + databasefile);
			Logger.LogMsg ("Loaded Database Handler.");

			if (!DoChecks ()) {//Do checks to make sure the server is ok.
				Logger.LogMsg ("Server cannot continue due to a failed check. The Server will close in 10 seconds"); //The server failed for a reason.
				Thread.Sleep (10000); //Give the user time to read the check log.
				System.Environment.Exit (0);
			}

			T_Server = new Thread (new ThreadStart (server.DoLogic)); //Start the server thread.
			T_Server.Start ();
			while (!T_Server.IsAlive) {
				System.Diagnostics.Debug.WriteLine("Server Thread not ready!");
				Thread.Sleep(25);
			}
			Thread.Sleep(250);
            while (T_Server.IsAlive) //While the server is running ok, ask for commands.
            {
                Console.Write(">");
                server.Command = Console.ReadLine();          
            }
        }

		public static void CloseServer (int timer = 5)
		{
			database.connection.Close ();
			DateTime startedtimer = DateTime.Now;
			server.Broadcast("[SAY]Server: The game will close in " + timer + " seconds");
			while ((DateTime.Now.TimeOfDay.TotalSeconds - startedtimer.TimeOfDay.TotalSeconds) < timer) {
				Thread.Sleep(250);
			}
			foreach (Player plr in server.MainGame.entityHandler.ents) {
				server.KickPlayer(plr,"Server closing down");
			}
			server.server.Stop();
			T_Logger.Abort();
			Environment.Exit(0);
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
		static List<string> buffer = new List<string>(64);
        public static string file = "";
        public static int loglevel = 1;

        public static void LogMsg (string msg, int level = 1)
		{
			if (loglevel < level) {
				return; //Ignore, we arn't checking.
			}

			buffer.Add(msg);
        }

		public static void DoLogger ()
		{
			while (true) {
				while (buffer.Count > 0) {
					try{
						stream = File.Open (file, FileMode.Append, FileAccess.Write);
					}
					catch(IOException ex)
					{
						Logger.LogMsg("Logger exception " + ex.Message,2); 
					}
					string msg = buffer.First();
					msg = "[" + DateTime.Now.ToShortTimeString () + "]" + msg + System.Environment.NewLine;
					Console.WriteLine (msg);
					byte[] bytes = ASCIIEncoding.ASCII.GetBytes (msg);
					stream.Write (bytes, 0, bytes.Length);
					stream.Flush ();
					LastMessage = msg;
					buffer.Remove(buffer.First());
					stream.Close ();
					Thread.Sleep (25);
				}
			}
		}
        public static string LastMessage = "";
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

}
