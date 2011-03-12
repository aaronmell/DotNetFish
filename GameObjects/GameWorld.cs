using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace DotNetFish.GameObjects
{
	/// <summary>
	/// Holds all of the inforamation about the Gameworld itself. 
	/// </summary>
	[Serializable()]
    public class GameWorld : ISerializable
    {
        public MapTile[,] GameMap { get; set; }

        public string Name { get; set; }

        public string Location { get; set; }

        public string Description { get; set; }

		public GameWorld()
		{
		}

		public GameWorld(SerializationInfo info, StreamingContext context)
		{
			Name = info.GetString("Name");
			Description = info.GetString("Location");
			GameMap = (MapTile[,])info.GetValue("GameMap", typeof(MapTile[,]));
		}
		
		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("Name", Name);
			info.AddValue("Location", Location);
			info.AddValue("Description", Description);
			info.AddValue("GameMap", GameMap);
		}
    }
}
