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
	class HUD : Screen2D
	{
		private int Score_v = 0;
		private int Lives_v = 0;

		/// <summary>
		/// Score.
		/// </summary>
		public int Score
		{
			get { return Score_v; }
			set
			{
				textureIsValid = false;
				Score_v = value;
			}
		}
		/// <summary>
		/// Lives.
		/// </summary>
		public int Lives
		{
			get { return Lives_v; }
			set
			{
				textureIsValid = false;
				Lives_v = value;
			}
		}

		protected override void render2D(Graphics gfx)
		{
			Font font = new Font(new FontFamily("Tahoma"), 24, FontStyle.Bold);
			SolidBrush brush = new SolidBrush(Color.Yellow);

			string score = "Score: " + Score.ToString();
			SizeF size = gfx.MeasureString(score, font);
			gfx.DrawString(score, font, brush, Width - size.Width, 0);


			score = "Lives: " + Lives.ToString();
			size = gfx.MeasureString(score, font);
			gfx.DrawString(score, font, brush, 0, 0);
		}
	}
}
