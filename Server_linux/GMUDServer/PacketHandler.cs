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
class PacketHandler
    {
        public static void DeterminePacket(string msg, WebSocketSession uc)
        {
            //[TPE]
            string determiner = msg.Substring(1, 3);
            string arguments = msg.Substring(5, msg.Length - 5);
			string[] argarray = arguments.Split(',');
			Player plr = Program.server.GetPlayerByIP(uc.RemoteEndPoint.Address + ":" + uc.RemoteEndPoint.Port);
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
                    Program.server.MainGame.entityHandler.AddEnt(newplayer);
					if(newplayer.banned)
						Program.server.KickPlayer(newplayer,"Your Banned");
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
					
					if(argarray[1] == "" || argarray[1].Length > 16){
							if(problems.Length > 0)
								problems += ",";
							problems += "3";	
					}

					if(argarray[2] == "" || argarray[2].Length > 16){
						if(problems.Length > 0)
							problems += ",";
						problems += "4";	
					}
					if(Program.database.DoesAccountExist(argarray[0]))
					{
						if(problems.Length > 0)
							problems += ",";
						problems += "1";	
					}
					if(string.IsNullOrWhiteSpace(problems)){
						
						bool worked = Program.database.AddPlayer(argarray[2],0);
						int id = Program.database.GetPlayerID(argarray[2]);
						if(id == -1){ worked = false;}
						if(worked)
							worked = Program.database.CreateAccount(argarray[0],argarray[1],id);
						
						if(worked){
							Logger.LogMsg("Account created", 3);
							uc.Send("[NEW]0");
						}
						else
						{
							Logger.LogMsg("Account creation failed. Database problems.", 3);
							uc.Send("[NEW]5");
						}
					}
					else{
						Logger.LogMsg("Account couldn't be created due to reasons " + problems, 3);
						uc.Send("[NEW]" + problems);
					}
					break;
				case "MAP":
					//Request for map data.
					string mapdata = MiscMethods.StringArrayToString(Program.server.MainGame.map.ToStringArray());
					uc.Send("[MAP]" + mapdata);
					break;
				case "RDY":
					//All data recieved, client is ready
					if(plr != null)
						plr.waitingToSpawn = true;
					break;
                default:
                    break;
            }
        }
    }
}