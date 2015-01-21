using System;
using System.Drawing;

namespace PacMan
{
	/// <summary>
	/// Additional info screen.
	/// </summary>
	class Info : Screen2D
	{
		/// <summary>
		/// Info strings.
		/// </summary>
		public string[] Items = null;

		protected override void render2D(Graphics gfx)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Nothing to render.");

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

