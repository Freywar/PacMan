using System.Drawing;

namespace PacMan
{
	/// <summary>
	/// Score and lives screen.
	/// </summary>
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
			Font font = new Font(fontFamily, 18, FontStyle.Regular);
			SolidBrush brush = new SolidBrush(Color.Yellow);

			string score = "Score: " + Score.ToString();
			SizeF size = gfx.MeasureString(score, font);
			gfx.DrawString(score, font, brush, Width - size.Width, 0);

			int x = 0;
			for (int i = 0; i < Lives; i++)
			{
				gfx.FillPie(brush, new Rectangle(x, 0, 24, 24), 45, 270);
				x += 24;
			}
		}
	}
}
