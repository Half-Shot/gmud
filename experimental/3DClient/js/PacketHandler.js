function DefinePacket(msg)
{
	var rest = msg.substring(5,msg.length);
	var type = msg.substring(1,4);

	var arguments;
	if(rest.indexOf(',') == -1)
	{
	arguments = rest;
	}
	else
	{
	arguments = rest.split(','); 
	}
	switch (type)
	{
		//Load Player Data
		case "LOD":
			console.log('Recieved player info for playerID' + playerID + ": " + rest);
			playerData = arguments;
			LogToGameConsole("Got playerdata. Enjoy your game. Type help for commands");
			isLoggedIn = true;
			Update();
			break;
		//Message from someone broadcasted
		case "UPP":
			playerData = arguments;
			//LogToGameConsole(rest);
			Update();
			break;
		case "SAY":
			console.log('Recieved chat message ' + rest);
			LogToGameConsole(rest,'Log');
			break;
		//ReadyToLogIn
		case "ACN":
			switch (rest)
			{
				case "002":
					LogToGameConsole("Couldn't find a account with that username.",'Log');
					break;
				case "003":
					LogToGameConsole("Incorrect Password.",'Log');
					break;
				default:
					DownloadUserData(rest);
					break;
			}
			break;
		case "MAP":
			if(mapdata == null) //Must be a new connection. 
				webSocket.send("[RDY]"); // The players got everything.
			
			lastmap = mapdata;
			mapdata = arguments;
			Update();
			break;
		default:
			console.log('WARNING: Unknown packet type ' + type);
			break;
	}
}
