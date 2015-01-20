using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Xml;
using System.IO;
using System.Drawing;

namespace PacMan
{
	class Info : Screen2D
	{
		public string[] Items;

		protected override void render2D(Graphics gfx)
		{
			Font font = new Font(fontFamily, 12, FontStyle.Regular);
			SolidBrush brush = new SolidBrush(Color.White);

			SizeF[] sizes = new SizeF[Items.Length];
			float totalHeight = 0;
			for (int i = 0; i < Items.Length; i++)
			{
				sizes[i] = gfx.MeasureString(Items[i], font);
				totalHeight += sizes[i].Height;
			}

			float y = Height - totalHeight;
			for (int i = 0; i < Items.Length; i++)
			{
				gfx.DrawString(Items[i], font, brush, 0, y);
				y += sizes[i].Height;
			}
		}
	}
}

