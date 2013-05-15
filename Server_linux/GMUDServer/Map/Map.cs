using System;
using System.Linq;
using System.Collections.Generic;
namespace GMUDServer
{

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
			//Set all tiles to soild
			for (int z = 0; z < worldmap.GetLength(2); z++) {
				for (int x = 0; x < worldmap.GetLength(0); x++) {
					for (int y = 0; y < worldmap.GetLength(1); y++) {
						worldmap [x, y, z] = '#';
					}		
				}

				//Create rooms
				for (int r = 0; r < rand.Next(150,300); r++) {
					int width = rand.Next (3, 15), height = rand.Next (3, 10);
					int cx = 0, cy = 0;
					bool failed = true;
					while (failed) {
						while ((cx - width) < 25 || (cx + width) > 486 || (cy - height) < 25 || (cy + height) > 486) {
							cx = rand.Next (0, 511);
							cy = rand.Next (0, 511);
						}
					
						Room potential = new Room(new Cord (cx, cy, z), width, height);
						if(r == 0){failed = false; };
						foreach (Room room in rooms) {
							if (!potential.Intersects (room))
								failed = false;
						}

					}
					Room newRoom = new Room(new Cord (cx, cy, z), width, height);


					if (rooms.Count () > 1) {
						//Create a tunnel.
						Room[] selectedr = FindNearestRooms(newRoom,Consts.MAX_TUNNEL_DIST);
					    Room oldRoom;
						if(selectedr.Count() > 0)
						{
							int roomid = rand.Next(1,selectedr.Count()) - 1;
							oldRoom = rooms[rooms.IndexOf(selectedr[roomid])];
	
							Cord oldCord = oldRoom.getCenter();
							Cord newCord = newRoom.getCenter();

							int minx = Math.Min(newCord.x,oldCord.x);
							int maxx = Math.Max(newCord.x,oldCord.x);

							int miny = Math.Min(newCord.y,oldCord.y);
							int maxy = Math.Max(newCord.y,oldCord.y);

							int oldy = oldCord.y;
							int oldx = oldCord.x;

							//Horizontal tunnel.
							for(int x = minx; x < maxx; x++) {
								worldmap[x,oldy,z] = '.';
							}
							//Vertical tunnel.
							for(int y = miny; y < maxy; y++) {
								worldmap[oldx,y,z] = '.';
							}
						}
					}
					ApplyRoom(newRoom);
					rooms.Add(newRoom);
				}
			}
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

		public void ApplyRoom (Room room)
		{
			for (int _y = 0; _y < room.height; _y++) {
				for (int _x = 0; _x < room.width; _x++) {
					worldmap [room.pos.x + _x, room.pos.y + _y, room.pos.z] = '.';
				}
			}
		}

		public void Load (string filepath)
		{
		}

		public Room[] FindNearestRooms (Room room, int distance)
		{
			List<Room> selected = new List<Room> ();
			foreach (Room otherRoom in rooms) {
				if(Cord.Distance(room.pos,otherRoom.pos) <= distance)
					selected.Add(otherRoom);
			}

			return selected.ToArray();
		}
	}

	class Room
	{

		public Room (Cord cord, int width, int height)
		{
			pos = cord;
			this.width = width;
			this.height = height;
		}
		public int width,height= 0;
		public Cord pos;
		public Cord getCenter()
		{
			return new Cord(pos.x + (width / 2),pos.y + (height / 2),pos.z);
		}

		public bool Intersects (Room otherRoom)
		{
			bool x_i = false,y_i = false;

			int r_xmin = pos.x;
			int r_xmax = pos.x + width;
			int r_ymin = pos.y;
			int r_ymax = pos.y + height;

			int or_xmin = otherRoom.pos.x;
			int or_xmax = otherRoom.pos.x + otherRoom.width;
			int or_ymin = otherRoom.pos.y;
			int or_ymax = otherRoom.pos.y + otherRoom.height;

			if (r_xmin >= or_xmin && r_xmin <= or_xmax) //Is the room along the x side.
				x_i = true;

			if(r_ymin >= or_ymin && r_ymin <= or_ymax)
				y_i = true;

			if(x_i && y_i)
				return true;
			else
				return false;
		}
	}

}

