using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperWebSocket;
namespace GMUDServer
{
    class Player : Entity
    {
		public int id, maxhealth, attackxp, defencexp, magicxp, kills, attacklevel, defencelevel, magiclevel;
		public string address;
        public bool admin;
        public bool banned;
        public WebSocketSession session;
        public Player()
        {
			waitingToSpawn = false;
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
                + position.ToString() + "," //8 //9 //10
                + attacklevel + "," //11
                + defencelevel + "," //12
                + magiclevel + "," //13
                + level + "," //14
                + MiscMethods.BoolToInt(admin) + "," //15
                + MiscMethods.BoolToInt(banned); //16
        }

		public override void Spawn (int health, Cord pos)
		{
			base.Spawn (health, pos);
			session.Send("[UPP]" + ToString());
		}

		public bool Move (Direction dir)
		{
			if(Program.server.MainGame.map.FindTileByDir(position,(int)dir,'.',1) == -1)
				return false;

			switch (dir) {
			case Direction.UP:
				position.y--;
				break;
			case Direction.RIGHT:
				position.x++;
				break;
			case Direction.DOWN:
				position.y++;
				break;
			case Direction.LEFT:
				position.x--;
				break;
			}

			return true;
		}
    }

    class Game
    {
		public Map map = new Map(512,512,1);
		public EntityHandler entityHandler;
		public Game ()
		{
			//Init
			map.Generate ();
			map.Save (Environment.CurrentDirectory + "/" + Program.MapFile);

			//
			//if (!System.IO.File.Exists (Environment.CurrentDirectory + "/" + Program.MapFile)) {
			//	map.Generate ();
			//} else 
			//	map.Load(Environment.CurrentDirectory + "/" + Program.MapFile);
			entityHandler = new EntityHandler();
			entityHandler.parent = this;
		}

        public void ExecuteGameCommand()
        {

        }

		public void StartGame()
		{
			SamGobson sam1 = new SamGobson(new Cord(0,0,0));
			sam1.name = "SamDrone";
			sam1.health = 100;
			for (int i = 0; i < 5; i++) {
				entityHandler.AddEnt(sam1);
			}

		}
    }

	class EntityHandler
	{
		private List<Entity> Ents = new List<Entity>();
		private List<Entity> entstoadd = new List<Entity>();
		private List<Entity> entstoremove = new List<Entity>();
		public Game parent;
		public List<Entity> ents {
			get {
				return Ents;
			}

		}


		public void AddEnt(Entity ent)
		{
			entstoadd.Add(ent);
			ent.parent = this;
		}

		public void RemoveEnt(Entity ent)
		{
			entstoremove.Add(ent);
		}

		public EntityHandler()
		{

		}

		public void Update ()
		{
			ents.AddRange(entstoadd);

			for (int i = 0; i < entstoremove.Count(); i++) {
				ents.Remove(entstoremove[i]);
			}

			entstoadd.Clear();
			entstoremove.Clear();

			for (int i = 0; i < ents.Count(); i++) {
				if(Ents[i].Spawned){
					Ents[i].Update();
					Ents[i].RunAI();
				}
				else if(Ents[i].waitingToSpawn)
				{
					Ents[i].Spawn(10);
				}
			}
			System.Threading.Thread.Sleep(Consts.EH_TIMEOUT);
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
			for (int i = 0; i < ents.Count(); i++) {
				Entity ent = ents[i];
				if(ent == input)
					continue;
				if(Distance(input,ent) < 25)
				{
					found.Add(ent);
				}
			}

			return found.ToArray();
		}

		public Player[] PlayerList()
		{
			Entity[] ents = Ents.ToArray();
			List<Player> plrs = new List<Player>();
			for (int i = 0; i < ents.Count(); i++) {
				Entity ent = ents[i];
				if(ent.GetType() == typeof(Player))
					plrs.Add((Player)ent);
			}

			return plrs.ToArray();
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
		public bool waitingToSpawn = true;
		public EntityHandler parent;
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

		public virtual void Spawn (int health, Cord pos = null)
		{
			this.health = health;
			Spawned = true;
			if (pos == null || pos == new Cord(0,0,0)) {
				Room room = Program.server.MainGame.map.rooms [rand.Next (0, Program.server.MainGame.map.rooms.Count - 1)]; //Get a random spawn room.
				position = new Cord(room.getCenter().x,room.getCenter().y,room.getCenter().z);
			}
			else
				position = pos;

			Logger.LogMsg("Spawned a " + name + " at " + position,2);
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

		public override void RunAI ()
		{
			if ((talktimer - DateTime.Now).Seconds > 10) {
				talktimer = DateTime.Now;
				foreach (Player plr  in Program.server.MainGame.entityHandler.ents) {
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

		public override string ToString ()
		{
			return x + "," + y + "," + z;
		}

		public static int Distance(Cord c1, Cord c2)
		{
			int width = Math.Max(c1.x,c2.x) - Math.Min(c1.x,c2.x);
			int height = Math.Max(c1.y,c2.y) - Math.Min(c1.y,c2.y);

			return (int)Math.Sqrt(Math.Pow(width,2) + Math.Pow(height,2));
		}
	}

	enum Direction
	{
		UP = 0,
		LEFT = 1,
		DOWN = 2,
		RIGHT = 3
	}
}
