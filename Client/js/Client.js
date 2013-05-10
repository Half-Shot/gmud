var version = 0.01;
var webSocket;
var playerID = -1;
var serverip;
var serverport;
var isLoggedIn = false;
var playerData;
var mapdata;
var plr_x = 10,plr_y = 10;
var conbox;
function main(serverip)
{
	if(validation == false){ return;}
	if(webSocket == null || webSocket.readyState != 1){
		serverip = document.getElementById("serverip").value;
		serverport = document.getElementById("serverport").value;
		console.log('Starting Client ' + version + ' using Server IP of ' + serverip);
		progressBox.innerHTML = "Connecting...";
		serverIP = serverip;
		conbox = document.getElementById("consoleBox");
		WebSocketSetup();
	}
	else
	{
		alert("You cannot have more than one login in the same window! (hint,hint) ");	
	}
}

function validation()
{
	var problems = "";
	if(document.getElementById("serverip").value == "")
		problems += "Please type a server IP/n";
	if(document.getElementById("serverport").value == "")
		problems += "Please type a server port/n";
	if(document.getElementById("username").value == "")
		problems += "Please type a username/n";
	if(document.getElementById("password").value == "")
		problems += "Please type a password/n";
	if(problems != "")
	{
		alert(problems);
		return false;
	}
	
	return true;
}
function onOpen()
{
	        // Web Socket is connected, send data using send()
		//Login Process
		sleep(2500);
		LogToGameConsole('Waiting for login details.','Log');
		webSocket.send("[ACN]" + document.getElementById("username").value + "," + document.getElementById("password").value);
		window.onbeforeunload = function() {
		    webSocket.onclose = function () {}; // disable onclose handler first
		    webSocket.close();
		};
}

function DownloadUserData(player)
{
		playerID = player;
		LogToGameConsole('Trying to get player info.','Log');
		progressBox.innerHTML = "Connected";
		webSocket.send("[LOD]" + playerID);
		webSocket.send("[MAP]NeedMapData");
		webSocket.send("[RDY]"); // The players got everything.
}

function onMsg(evt) 
{ 
		//LogToGameConsole('New Message: ' + evt.data,'Log');
		DefinePacket(evt.data);
		
		if(isLoggedIn)
			Update();
		
		conbox.scrollTop = conbox.scrollHeight; //Scroll box down when a new message appears.
}

function onClose()
{
	alert("Connection closed"); 
	progressBox.innerHTML =  "Not Connected";
}

function onError(evt)
{
	if(webSocket.readyState == 0)
	{
	alert("Sorry, The Server Is Offline.");
	}
	progressBox.innerHTML =  "Error";
}

function WebSocketSetup()
{
  if ("WebSocket" in window)
  {
     console.log('WebSocket Supported :D');
	 LogToGameConsole('Attempting connection to Server','Log');
	 progressBox.innerHTML =  'Attempting connection to Server';
     webSocket = new WebSocket("ws://" + serverIP + ":" + serverport);
     webSocket.onopen = onOpen;
     webSocket.onmessage = onMsg;
     webSocket.onclose = onClose;
	 webSocket.onerror = onError;
  }
  else
  {
     // The browser doesn't support WebSocket
     alert("Sorry, but you can't use this game on this browser. You need WebSocket Support");
  }
}

function sleep(delay) {
        var start = new Date().getTime();
        while (new Date().getTime() < start + delay);
}

function sendCommand()
{
	if(webSocket == null || webSocket.readyState != 1)
	{
		LogToGameConsole("Client not connected.\n",'Log');
		return;
	}

	var command = document.getElementById("combox").value;
	if(command == "")
	{
		LogToGameConsole("Command not specified.\n",'Log');
		return;
	}
	LogToGameConsole(command + "\n",'Log');
	document.getElementById("combox").value = "";
	webSocket.send("[CMD]" + command);

}

function Update()
{
	if(mapdata !== "undefined"){
	Minimap();
	}
	UpdatePlayerStats();
	plr_x = playerData[8];
	plr_y = playerData[9];
}

function LogToGameConsole(msg, tpe)
{
	conbox.innerHTML += "<div class='ConsoleMsg" + tpe +"'>" + msg + "</div><br>";
}
