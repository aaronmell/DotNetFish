using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameObjects;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace LevelBuilder
{
	public static class FileIO
	{
		public static void SaveMap(string filePath, GameWorld world)
		{
			Stream stream = File.Open(filePath, FileMode.Create);
			BinaryFormatter bf = new BinaryFormatter();

			bf.Serialize(stream, world);
			stream.Close();
		}

		public static GameWorld LoadMap(string filePath)
		{
			Stream stream = File.Open(filePath, FileMode.Open);
			BinaryFormatter bf = new BinaryFormatter();

			GameWorld gameWorld = (GameWorld)bf.Deserialize(stream);
			stream.Close();

			return gameWorld;
		}
	}
}
