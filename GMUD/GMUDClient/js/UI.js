function textboxhide(event)
{
	if(event.target.value == event.target.defaultValue)
	{
		event.target.value = "";
	}
}

function Minimap()
{
	var line = "";
	var x = plr_x;
	var sizemap = 10;
	$(".minimap").empty();
	for (var y = plr_y - sizemap; y < plr_y + sizemap; y++)
	{
		if( y < 0 || (x- sizemap) < 0){line += "<div class='tile_w'>" + mapline[i] + "</div>";}
		var mapline = mapdata[y].substring(x - sizemap,x + sizemap);
		for (var i=0;i<mapline.length;i++){
			switch(mapline[i]){
				case "#":
					line += "<div class='tile_w'>" + mapline[i] + "</div>";
					break;
				case ".":
					line += "<div class='tile_f'>" + mapline[i] + "</div>";
					break;
				case "D":
					line += "<div class='tile_d'>" + mapline[i] + "</div>";
					break;
			}
		}
		line += "<br>";
		//LogToGameConsole(HTMLLine,'Log');
		
	}
	$(".minimap").append(line);
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

$('#combox').bind('keyup', function(e) {

    if ( e.keyCode == 13 ) { // 13 is enter key
	sendCommand();
    }

});

