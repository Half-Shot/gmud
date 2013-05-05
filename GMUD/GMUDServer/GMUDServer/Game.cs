using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperWebSocket;
namespace GMUDServer
{
    class Player : Entity
    {
        public int id, maxhealth, health, attackxp, defencexp, magicxp, kills, attacklevel, defencelevel, magiclevel, level;

        public string name;
		public string address;
        public bool admin;
        public bool banned;
        public WebSocketSession session;
        public Player()
        {

        }

        public Player(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public Player(int id, string name, int health, int attackxp, int defencexp, int magicxp)
        {
            this.id = id;
            this.name = name;
            this.health = health;
            this.attackxp = attackxp;
            this.defencexp = defencexp;
            this.magicxp = magicxp;
        }

        public Player(int id, string name, int health, int attackxp, int defencexp, int magicxp,Cord pos): base(pos)
        {
            this.id = id;
            this.name = name;
            this.health = health;
            this.attackxp = attackxp;
            this.defencexp = defencexp;
            this.magicxp = magicxp;
        }

        public override string ToString()
        {
            return id + "," //0
                + name + ","  //1 
                + maxhealth + "," //2
                + health + "," //3
                + attackxp + "," //4
                + defencexp + "," //5
                + magicxp + "," //6
                + kills + "," //7
                + position.x + "," //8
                + position.y + "," //9
                + position.z + "," //10
                + attacklevel + "," //11
                + defencelevel + "," //12
                + magiclevel + "," //13
                + level + "," //14
                + MiscMethods.BoolToInt(admin) + "," //15
                + MiscMethods.BoolToInt(banned); //16
        }

		public override void Spawn (int health)
		{
			base.Spawn (health);
		}
    }

    class Game
    {
		public Map map = new Map(512,512,1);
		public EntityHandler entityHandler;
		public Game ()
		{
			//Init
			map.Generate();
			map.Save(Environment.CurrentDirectory + "/" + "default.map");
			entityHandler = new EntityHandler();
		}

        public void ExecuteGameCommand()
        {

        }
    }

	class Map
	{
		char[,,] worldmap;
		public List<Room> rooms = new List<Room>();
		List<Cord> doors = new List<Cord>();
		public Map (int width, int height, int depth)
		{
			 worldmap = new char[width,height,depth];
		}

		public void SetTile(Cord pos, char type)
		{
			worldmap[pos.x,pos.y,pos.z] = type;
		}

		public bool TileIsOnEdge (Cord pos)
		{
			if (pos.x == 0 || pos.x == worldmap.GetLength(0) - 1 || pos.y == 0 || pos.y == worldmap.GetLength(1) - 1)
				return true;
			return false;
		}

		public int IsTouching (Cord pos, char type)
		{
			int touches = 0;
			if (worldmap [pos.x - 1, pos.y, pos.z] == type) {
				touches++;
			} // Left

			if (worldmap [pos.x + 1, pos.y, pos.z] == type) {
				touches++;
			} // Right

			if (worldmap [pos.x, pos.y + 1, pos.z] == type) {
				touches++;
			} // Down

			if (worldmap [pos.x, pos.y - 1, pos.z] == type) {
				touches++;
			} // Up

			return touches;
		}

		public int FindTileByDir (Cord pos, int direction, char type, int maxreach = 200)
		{
			for (int i = 0; i < maxreach; i++) {
				switch (direction) {
				case 0:
					try {
						if(worldmap[pos.x,pos.y - i,pos.z] == type)
							return i;
					} catch (Exception ex) {
						return -1;
					}
					break;
				case 1:
					try {
						if(worldmap[pos.x + i,pos.y,pos.z] == type)
							return i;
					} catch (Exception ex) {
						return -1;
					}
					break;
				case 2:
					try {
						if(worldmap[pos.x,pos.y + i,pos.z] == type)
							return i;
					} catch (Exception ex) {
						return -1;
					}
					break;
				case 3:
					try {
						if(worldmap[pos.x - i,pos.y,pos.z] == type)
							return i;
					} catch (Exception ex) {
						return -1;
					}
					break;
				default:
					return -1;
					break;
				}
			}

			return -1;
		}

		public void Generate ()
		{
			Random rand = new Random ();
			for (int z = 0; z < worldmap.GetLength(2); z++) {
				for (int x = 0; x < worldmap.GetLength(0); x++) {
					for (int y = 0; y < worldmap.GetLength(1); y++) {
						worldmap [x, y, z] = '#';
					}
					
				}

				//Create rooms
				for (int actions = 0; actions < rand.Next(150,300); actions++) {
					int width = rand.Next (1, 15), height = rand.Next (1, 10);
					int cx = 0, cy = 0;
					while(cx - width < 25 || cx + width > 486 || cy - height < 25 || cy + height > 486)
					{
						cx = rand.Next(0,511);
						cy = rand.Next(0,511);
					}
					int direction = rand.Next (0, 4);
					rooms.Add(CreateRoom(new Cord(cx,cy,z),width,height));
				}
				foreach(Room room in rooms)
					{
						if(room.height < 3 || room.width < 3) //Room too small to be usable.
							continue;

						List<Cord> walls = new List<Cord>();
						for (int _x = room.x - room.width - 1; _x < room.x + room.width + 1; _x++) {
							walls.Add(new Cord(_x,room.y - room.width,z));
							walls.Add(new Cord(_x,room.y + room.width,z));
						}
						
						for (int _y = room.y - room.height - 1; _y < room.y + room.height + 1; _y++) {
							walls.Add(new Cord(room.x -room.width,_y,z));
							walls.Add(new Cord(room.x + room.width,_y,z));
						}
						
						Cord doorpos = walls[rand.Next(0,walls.Count() - 1)];
						doors.Add(doorpos);
						while(IsTouching(doorpos,'.') > 2 || IsTouching(doorpos,'#') > 3) //A door should never touch more than 2 of the floor and no more than 3 of the walls.
						{
							doorpos = walls[rand.Next(0,walls.Count() - 1)];
						}
						worldmap[doorpos.x,doorpos.y,doorpos.z] = 'D';
					}
					//Save(System.Environment.CurrentDirectory + "/" + "nopaths.map");
					foreach(Cord door in doors)
					{
						if(worldmap[door.x,door.y,door.z] != 'D')
							continue;
						//Get forward direction.
						int dir = 2; //0=up
						if(worldmap[door.x + 1,door.y,door.z] == '.')
							dir = 3;
						else if(worldmap[door.x,door.y + 1,door.z] == '.')
							dir = 0;
						else if(worldmap[door.x - 1,door.y,door.z] == '.')
					        dir = 1;
						
					   	
					int distance = FindTileByDir(door,dir,'.',1000);
					if(distance == -1)
					{
						worldmap[door.x,door.y,door.z] = '#';
					}
					else
					{
						switch(dir){
							case 0:
								for (int i = 1; i < distance; i++) {
								worldmap[door.x - 1,door.y - i,door.z] = '#';
								worldmap[door.x,door.y - i,door.z] = '.';
								worldmap[door.x + 1,door.y - i,door.z] = '#';
							    }
								break;
							case 1:
								for (int i = 1; i < distance; i++) {
								worldmap[door.x + i,door.y - 1,door.z] = '#';
								worldmap[door.x + i,door.y,door.z] = '.';
								worldmap[door.x + i,door.y + 1,door.z] = '#';
							    }
								break;
							case 2:
								for (int i = 1; i < distance; i++) {
								worldmap[door.x - 1,door.y + i,door.z] = '#';
								worldmap[door.x,door.y + i,door.z] = '.';
								worldmap[door.x + 1,door.y + i,door.z] = '#';
							    }
								break;
							case 3: //Left
								for (int i = 1; i < distance; i++) {
								worldmap[door.x - i,door.y - 1,door.z] = '#';
								worldmap[door.x - i,door.y,door.z] = '.';
								worldmap[door.x - i,door.y + 1,door.z] = '#';
							    }
								break;
						}
					}   
					}
			}
		}
		private Room CreateRoom (Cord cord,int width, int height)
		{
					//Floor
					for (int _y = -height; _y < height; _y++) {
						for (int _x = -width; _x < width; _x++) {
							worldmap [cord.x + _x, cord.y + _y, cord.z] = '.';
						}
					}
			Room room = new Room();
			room.x = cord.x;
			room.y = cord.y;
			room.z = cord.z;
			room.width = width;
			room.height = height;
			return room;
		}

		public string[] ToStringArray()
		{
			List<string> lines = new List<string>();
			for (int z = 0; z < worldmap.GetLength(2); z++) {
				for (int y = 0; y < worldmap.GetLength(1); y++) {
					string line = "";
					for (int x = 0; x < worldmap.GetLength(0); x++) {
						line += worldmap[x,y,z];
					}
					lines.Add(line);
				}
				lines.Add("BREAK");
			}

			return lines.ToArray();
		}

		public void Save (string filepath)
		{
			System.IO.File.WriteAllLines(filepath,ToStringArray());

		}
	}

	class Room
	{
		public int width,height,x,y,z = 0;
	}

	class EntityHandler
	{
		List<Entity> ents = new List<Entity>();
		public EntityHandler()
		{

		}

		public void Update()
		{
			foreach (Entity ent in ents) {
				if(ent.Spawned){
				ent.Update();
				ent.RunAI();
				}
				else
				{
					ent.Spawn(10);
				}
			}
		}

		public int Distance(Entity e_from, Entity e_to)
		{
			int distance_x = -(e_from.position.x - e_to.position.x);
			int distance_y = -(e_from.position.y - e_to.position.y);

			return (int)Math.Sqrt(Math.Pow(distance_x,2) + Math.Pow(distance_y,2));
		}

		public Entity[] Scan(Entity input, int distance = 25)
		{
			List<Entity> found = new List<Entity>();
			foreach (Entity ent in ents) {
				if(Distance(input,ent) < 25)
				{
					found.Add(ent);
				}
			}

			return found.ToArray();
		}
	}

	class Entity
	{
		public Random rand = new Random();
		public Cord position;
		public bool Spawned= false;
		public int health;
		public int level;
		public string name;
		public char letter;
		public bool isinCombat;
		public Entity ()
		{
			position = new Cord(0,0,0);
		}

		public Entity(Cord pos)
		{
			position = pos;
		}

		public virtual void Update()
		{

		}

		public virtual void Spawn(int health)
		{
			this.health = health;
			Spawned = true;
			Room room = Program.server.MainGame.map.rooms[rand.Next(0,Program.server.MainGame.map.rooms.Count - 1)];
			position.x = room.x;
			position.y = room.y;
			position.z = room.z;

		}

		public virtual void RunAI()
		{

		}
	}

	class SamGobson : Entity
	{
		DateTime talktimer = DateTime.Now;
		string[] quotes = new string[3];
		public SamGobson(Cord pos) : base(pos)
		{
			letter = 'S';
			health = 10;
			level = 1;
			name = "Sam Gobson";
			quotes[0] = "How did i end up here?";
			quotes[1] = "Fight me irl.";
			quotes[1] = "Subway!";
		}

		public virtual void RunAI ()
		{
			if ((talktimer - DateTime.Now).Seconds > 10) {
				talktimer = DateTime.Now;
				foreach (Player plr  in Program.server.connected_players) {
					if(Program.server.MainGame.entityHandler.Distance(plr,this) < 25)
					{
						plr.session.Send("[SAY]" + quotes[rand.Next(0,2)]);
					}
				}

			int direction = rand.Next(0,3);
			int movedist = rand.Next(0,3);

				switch (direction) {
				case 0:
					position.y -= direction;
					break;
				case 1:
					position.x += direction;
					break;
				case 2:
					position.y += direction;
					break;
				case 3:
					position.x -= direction;
					break;
				}
			}
		}
	}

	class Cord
	{
		public int x,y,z;
		public Cord(int x, int y, int z)
		{
			this.x = x;
			this.y = y;
			this.z = z;
		}
	}
}
