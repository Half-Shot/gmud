var serverValid = true;
var webSocket;
function testServer()
{

}

function createPlayer()
{
	var ip = $("#ipaddress").val();
	var port = $("#port").val();
	var acc = $("#charname").val();
	var usr = $("#username").val();
	var pwd = $("#password").val();
	webSocket = new WebSocket("ws://" + ip + ":" + port);
	webSocket.onmessage = onMsg;
	webSocket.onclose = function() { alert("Server not up!"); };
	webSocket.onopen = function() {
	webSocket.send("[NEW]" + usr + "," + pwd + "," + acc);};

}

function sleep(delay) {
        var start = new Date().getTime();
        while (new Date().getTime() < start + delay);
}

function onMsg(evt)
{
	webSocket.onclose = null;
	var msg = evt.data;
	var rest = msg.substring(5,msg.length);
	var arguments = new Array();
	if(rest.indexOf(',') == -1)
	{
		arguments[0] = rest;
	}
	else
	{
		arguments = rest.split(','); 
	}
	if(msg.substring(1,4) == "NEW")
	{
		var msg = "";
		if(arguments[0] == "0"){
			msg = "You win. Your being redirected to the game page.";}
		else{
			msg = "You dune goofed:";
			var length = arguments.length,
			element = null;
			for (var i = 0; i < length; i++) {
			  element = arguments[i];
			  	switch(element){
				case "1":
					msg += "An account with that username already exists.";
					break;
				case "2":
					msg += "The username is invalid.";
					break;
				case "3":
					msg += "The password is invalid.";
					break;
				case "4":
					msg += "The account name is invalid.";
					break;
				case "5":
					msg = "Coudln't create account due to server issues. Please wait while they are fixed.";
					break;
				}
			
			}


		}

		alert(msg);
		webSocket.close();
	}
}
