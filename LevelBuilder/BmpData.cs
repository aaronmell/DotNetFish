using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

namespace DotNetFish.LevelBuilder
{
	public class BmpData : IDisposable
	{
		private BitmapData _bitmapData;
		private byte[] _rgbValues;
		private Bitmap _currentTileBitmap;
		private bool disposed;

		public BmpData(Bitmap currentTileBitmap, int tileSize)
		{
			_currentTileBitmap = currentTileBitmap;

			//To reduce the processing time when determining the tile type we are going to use BitmapData here and an array, since it is much faster that using getpixel and setpixel.
			//We will check each edge of the tile for water and land. If only water is found we will set the tile to the water tile. If only land is found we will set the tile 
			//to the land tile. If both are found then we will call a routine that will figure out which tile to use.
			_bitmapData = currentTileBitmap.LockBits(
			new Rectangle(0, 0, tileSize, tileSize),
			ImageLockMode.ReadOnly,
			PixelFormat.Format32bppArgb);

			//This is a pointer that referenece the location of the first pixel of data 
			System.IntPtr Scan0 = _bitmapData.Scan0;

			//calculate the number of bytes
			int bytes = (_bitmapData.Stride * _bitmapData.Height);

			//An array of bytes. Just remember that each pixel format has a different number of bytes.
			//In our case, the number of bytes is 4 per pixel or RGBA. 
			_rgbValues = new byte[bytes];

			//Safely copying the data to a managed array
			System.Runtime.InteropServices.Marshal.Copy(Scan0,
						   _rgbValues, 0, bytes);

			Scan0 = IntPtr.Zero;
		}
	
		public BitmapData BitmapData
		{
			get
			{
				return _bitmapData;
			}
		}

		public byte[] RgbValues
		{
			get
			{
				return _rgbValues;
			}
		}

		public void Dispose()
		{
			Dispose(true);			
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					_currentTileBitmap.UnlockBits(_bitmapData);
				}			
			}
			disposed = true;
		}
	}
}
