//Graphics.js by Sharpened Studios.
//Using Three.js, a brilliant WebGL lib.
// - assume we've got jQuery to hand
var container = $('body');
var renderer = new THREE.WebGLRenderer();
var camera;
var scene = new THREE.Scene();

//Few constant variables
var tilewidth = 100;
var tileheight = 100;
var tiledepth = 10;

var wallwidth = 100;
var wallheight = 400;
var walldepth = 20;

var textures = new Array();
var hasLoaded = false;
function ResourceLoad()
{
	var monitor = new THREE.LoadingMonitor();
	var tload = new THREE.TextureLoader();

	tload.addEventListener  ('load',
					function(texture){ textures.push(texture.content);}
				);
	monitor.addEventListener('progress',
					function(arg){
								if(arg.loaded == arg.total){ 
								alert("Load Complete"); 
								GameInit(); 
								$('#loading').hide()
								}
							}
				);
	monitor.addEventListener('error',
					 function(msg){ alert(msg);}
				);

	tload.load('textures/floor1.jpg'); //Floor Dirty
	tload.load('textures/wall1.jpg');  //Wall Dirty
	tload.load('textures/floorclean.png');  //Floor Clean
	monitor.add( tload );
}
function CreateFloorTile(x,y,z)
{
	var material = new THREE.MeshLambertMaterial( {map: textures[0]} );
	//alert(material);
	// Set tile depth		
	// create a new mesh with
	// sphere geometry - we will cover
	// the sphereMaterial next!
	var tile = new THREE.Mesh(
			new THREE.CubeGeometry( tilewidth, tileheight, tiledepth ),
			material);

	tile.position.x = x * tilewidth;
	tile.position.y = y * tileheight;
	tile.position.z = z * wallheight;
	tile.rotation.x = 0;
	tile.overdraw = true;
	// add the sphere to the scene
	scene.add(tile);
}

function CreateWall(x,y,z,direction)
{
var material = new THREE.MeshLambertMaterial( {map: textures[1]} );
var wall = new THREE.Mesh(
		new THREE.CubeGeometry( walldepth, wallwidth, wallheight ),
		material);
		wall.position.x = x * tilewidth;
		wall.position.y = y * tileheight;
switch(direction){
	case 'left':
		wall.position.x -= tilewidth / 2;
		//wall.position.y -= tileheight / 2;
		break;
	case 'right':
		wall.position.x += tilewidth / 2;
		//wall.position.y += tileheight / 2;
		break;

	case 'top':
		wall.rotation.z = 1.570796325;	
		wall.position.y -= tileheight / 2;	
		break;
	case 'bottom':
		wall.rotation.z = -1.570796325;
		wall.position.y += tileheight / 2;
		break;
}

wall.position.z += (z * wallheight / 2) + (wallheight / 2);
scene.add(wall);
}

function CreateLight(x,y,z,colour)
{
	// create a point light
	var pointLight =
	  new THREE.PointLight(colour);

	// set its position
	pointLight.position.x = x;
	pointLight.position.y = y;
	pointLight.position.z = z;

	// add to the scene
	scene.add(pointLight);
}

function GameInit()
{
	// set the scene size
	var WIDTH = 1366,
	    HEIGHT = 768;

	// set some camera attributes
	var VIEW_ANGLE = 45,
	    ASPECT = WIDTH / HEIGHT,
	    NEAR = 0.1,
	    FAR = 10000;

	camera =
	  new THREE.PerspectiveCamera(
	    VIEW_ANGLE,
	    ASPECT,
	    NEAR,
	    FAR);

	scene.add(camera);

	// the camera starts at 0,0,0
	// so pull it back
	camera.position.z = 1000;
	camera.position.y = -50;
	camera.position.x = 0;
	camera.rotation.x = 0;
	// start the renderer
	renderer.setSize(WIDTH, HEIGHT);

	// attach the render-supplied DOM element
	container.append(renderer.domElement);
	//CreateFloorTile(0,0,0);
	//CreateFloorTile(1,0,0);
	//CreateFloorTile(0,1,0);
	//CreateFloorTile(1,1,0);
	//CreateFloorTile(2,0,0);
	//CreateFloorTile(3,0,0);
	//CreateWall(0,0,0,'left');
	//CreateWall(0,1,0,'left');
	//CreateWall(3,0,0,'right');
	//CreateWall(0,0,0,'bottom');
	//CreateWall(1,0,0,'bottom');
	//CreateWall(2,0,0,'bottom');
	//CreateWall(3,0,0,'bottom');
	//CreateWall(0,1,0,'top');
	//CreateWall(1,1,0,'top');
	//CreateWall(2,0,0,'top');
	//CreateWall(3,0,0,'top');
	//CreateWall(1,1,0,'right');
	// add subtle blue ambient lighting
	//CreateLight(50,50,200,'#FBB117');
	var mapdata = new Array();
	mapdata.push("########");
	mapdata.push("#......#");
	mapdata.push("#......#");
	mapdata.push("#......#");
	mapdata.push("########");
	CreateFromMapData(mapdata)
        var ambientLight = new THREE.AmbientLight('#FBB117');
        scene.add(ambientLight);CreateFromMapData(mapdata)

	// draw!	
	renderer.render(scene, camera); 
}

function CreateFromMapData(mapdata)
{
	for (var y=0;y<mapdata.length;y++)
	{
		for (var x=0;x<mapdata[y].length;x++)
		{
			var type = mapdata[y].charAt(x);
			switch(type){
				case "#":
					break;
				
				case ".":
					CreateFloorTile(x,y,0);
					var Walls = GetWallDirections(x,y,mapdata);
					if(Walls.charAt(0) == '1')
						CreateWall(x,y,0,'left');
					if(Walls.charAt(1) == '1')
						CreateWall(x,y,0,'right');
					if(Walls.charAt(2) == '1')
						CreateWall(x,y,0,'top');
					if(Walls.charAt(3) == '1')
						CreateWall(x,y,0,'bottom');	
					console.log(Walls);					
					break;
			}
		}	
	}
}

function GetWallDirections(x,y,mapdata)
{
	var directions = "";
	//LEFT
	if(mapdata[y].charAt(x - 1) == "#")
		directions += "1";
	else
		directions += "0";

	//RIGHT
	if(mapdata[y].charAt(x + 1) == "#")
		directions += "1";
	else
		directions += "0";

	//UP
	if(mapdata[y - 1].charAt(x) == "#")
		directions += "1";
	else
		directions += "0";

	//DOWN
	if(mapdata[y + 1].charAt(x) == "#")
		directions += "1";
	else
		directions += "0";
	
	return directions;	
}
