using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace DotNetFish.GameObjects
{
	[Serializable]
	public class EdgeConnection
	{
		public EdgeConnection(byte edgePosition, bool isConnected)
		{
			EdgePosition = edgePosition;
			IsConnected = isConnected;
		}

		public EdgeConnection(byte edgePosition) : this(edgePosition, false) { }

		public EdgeConnection() : this(0,true) { }		

		public byte EdgePosition { get; set; }
		[XmlIgnore]
		public bool IsConnected { get; set; }

		public byte TranslateConnectionForNeighbor()
		{
			switch (EdgePosition)
			{
				case 1:
					return 9;						
				case 2:
					return 8;						
				case 3:
					return 7;						
				case 4:
					return 12;
				case 5:
					return 11;
				case 6:
					return 10;
				case 7:
					return 3;
				case 8:
					return 2;
				case 9:
					return 1;
				case 10:
					return 6;
				case 11:
					return 5;
				case 12:
					return 4;
			default:
				throw new ArgumentOutOfRangeException();
			}			
		}		
	}
}
