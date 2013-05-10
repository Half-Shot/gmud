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
	class MainServer
    {
        public WebSocketServer server;
		public Game MainGame;
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
			MainGame.entityHandler.AddEnt(new SamGobson(null));
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
			Program.CloseServer();
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
			string address = session.RemoteEndPoint.Address + ":" + session.RemoteEndPoint.Port;
			Player toRemove = GetPlayerByIP(address);
			MainGame.entityHandler.RemoveEnt(toRemove);
			Logger.LogMsg (toRemove.name + " disconnected from the server. Reason " + reason.ToString ());
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
					ShouldWork = false;
                    break;
                case "quit":
                    Logger.LogMsg("Closing Server : From Console");
                    ShouldWork = false;
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
            foreach (Player plr in MainGame.entityHandler.PlayerList())
                plr.session.Send(msg);
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
					bool moved = false;;
					switch(arguments)
					{
						case "left":
							moved = plr.Move(Direction.LEFT);
							break;
						case "right":
							moved = plr.Move(Direction.RIGHT);
							break;
						case "up":
							moved = plr.Move(Direction.UP);
							break;
						case "down":
							moved = plr.Move(Direction.DOWN);
							break;
						default:
							
							break;
					}
					session.Send("[UPP]" + plr.ToString()); //Update player
					if(!moved){session.Send("[SAY]Somthing is blocking your path.");}
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

		public Player GetPlayerByIP (string ip)
		{	
			foreach (Player plr in MainGame.entityHandler.PlayerList()) {
				if (plr.address == ip)
					return plr;
			}
			return null;
		}

		public void KickPlayer(Player plr,string reason)
		{
			plr.session.Send("[SAY] Kicked from server. Reason " + reason);
			plr.session.CloseWithHandshake(reason);
		}
    }
}