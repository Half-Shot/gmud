var version = 0.01;
var webSocket;
var playerID = -1;
var serverip;
var serverport;
var isLoggedIn = false;
var playerData;
var mapdata;
var plr_x = 10,plr_y = 10;
function main(serverip)
{
	serverip = document.getElementById("serverip").value;
	serverport = document.getElementById("serverport").value;
	console.log('Starting Client ' + version + ' using Server IP of ' + serverip);
	progressBox.innerHTML = "Connecting...";
	serverIP = serverip;
	WebSocketSetup();
}

function onOpen()
{
	        // Web Socket is connected, send data using send()
		//Login Process
		if(document.getElementById("username").value == "" || document.getElementById("password").value == "")
		{
			alert("Please type a username and password!");
			webSocket.close();
			return;
		}
		sleep(2500);
		LogToGameConsole('Waiting for login details.','Log');
		webSocket.send("[ACN]" + document.getElementById("username").value + "," + document.getElementById("password").value);
		$("#loginbutton").attr('disabled','disabled');
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
}

function onMsg(evt) 
{ 
		//LogToGameConsole('New Message: ' + evt.data,'Log');
		DefinePacket(evt.data);
		Update();
}

function onClose()
{
	alert("Connection closed"); 
	progressBox.innerHTML =  "Not Connected";
	$("#loginbutton").removeAttr("disable");
}

function onError(evt)
{
	if(webSocket.readyState == 0)
	{
	alert("Sorry, The Server Is Offline.")
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
	Minimap();
	UpdatePlayerStats();
}

function LogToGameConsole(msg, tpe)
{
consoleBox.innerHTML += "<div class='ConsoleMsg" + tpe +"'>" + msg + "</div><br>";
}
