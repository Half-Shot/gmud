function textboxhide(event)
{
	if(event.target.value == event.target.defaultValue)
	{
		event.target.value = "";
	}
}

function Update()
{
	
}

function UpdatePlayerStats()
{
	$("#health").html(playerData[3] + "/" + playerData[2]);
	$("#level").html(playerData[14]);
	$("#kills").html(playerData[7]);
	$("#dungeon").html(playerData[10]);

	$("#attacklvl").html(playerData[11]);
	$("#attackxp").html(playerData[4]);

	$("#defencelvl").html(playerData[12]);
	$("#defencexp").html(playerData[5]);

	$("#magiclvl").html(playerData[13]);
	$("#magicxp").html(playerData[6]);
}

$('#coninput').bind('keyup', function(e) {

    if ( e.keyCode == 13 ) { // 13 is enter key
	sendCommand();
    }

});

