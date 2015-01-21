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
	class Menu : Screen2D
	{
		public struct Item
		{
			public string Text;
			public bool Enabled;
			public Item(string text, bool enabled)
			{
				Text = text;
				Enabled = enabled;
			}
		}

		/// <summary>
		/// Строки заголовка.
		/// </summary>
		public string[] Header = null;

		/// <summary>
		/// Элементы меню.
		/// </summary>
		public Item[] Items = null;
		private int selectedIndex = 0;

		private Key? pressedKey = null;
		private double pressedKeyTime = 0;

		public void Init()
		{
			pressedKey = null;
			pressedKeyTime = 0;
			selectedIndex = 0;
		}

		public void Update(double dt)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not navigate empty menu.");

			if (pressedKey != null)
				pressedKeyTime += dt;

			if (pressedKeyTime >= 0.3)
			{
				if (pressedKey == Key.Up && selectedIndex > 0)
				{
					selectedIndex--;
					textureIsValid = false;
				}

				if (pressedKey == Key.Down && selectedIndex < Items.Length - 1)
				{
					selectedIndex++;
					textureIsValid = false;
				}

				pressedKeyTime = 0;
			}
		}

		public int? KeyDown(Key key)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not navigate empty menu.");

			if (key == Key.Up || key == Key.Down)
			{
				pressedKey = key;
				pressedKeyTime = 0.5;
			}

			if (key == Key.Enter && Items[selectedIndex].Enabled)
				return selectedIndex;
			else
				return null;
		}

		public int? KeyUp(Key key)
		{
			if (key == pressedKey)
			{
				pressedKey = null;
				pressedKeyTime = 0;
			}
			return null;
		}

		protected override void render2D(Graphics gfx)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not render empty menu.");

			Font headerFont = new Font(fontFamily, 48, FontStyle.Bold);
			SolidBrush headerBrush = new SolidBrush(Color.Yellow);

			SizeF[] headerSizes = new SizeF[0];
			float totalHeaderHeight = 0;
			if (Header != null)
			{
				headerSizes = new SizeF[Header.Length];
				for (int i = 0; i < Header.Length; i++)
				{
					headerSizes[i] = gfx.MeasureString(Header[i], headerFont);
					totalHeaderHeight += headerSizes[i].Height;
				}
				totalHeaderHeight+=headerSizes[0].Height;
			}

			Font font = new Font(fontFamily, 24, FontStyle.Regular);
			SolidBrush selectedEnabledBrush = new SolidBrush(Color.Yellow);
			SolidBrush selectedDisabledBrush = new SolidBrush(Color.FromArgb(64, Color.Yellow));
			SolidBrush enabledBrush = new SolidBrush(Color.Blue);
			SolidBrush disabledBrush = new SolidBrush(Color.FromArgb(64, Color.Blue));

			SizeF[] sizes = new SizeF[Items.Length];
			float totalHeight = 0;
			for (int i = 0; i < Items.Length; i++)
			{
				sizes[i] = gfx.MeasureString(Items[i].Text, font);
				totalHeight += sizes[i].Height;
			}

			float x = Width / 2,
				y = Height / 2 - (totalHeaderHeight + totalHeight) / 2;
			if (Header != null)
			{
				for (int i = 0; i < Header.Length; i++)
				{
					gfx.DrawString(Header[i], headerFont, headerBrush, x - headerSizes[i].Width / 2, y);
					y += headerSizes[i].Height;
				}
			}

			x = Width / 2;
			y = Height / 2 - (totalHeaderHeight + totalHeight) / 2 + totalHeaderHeight;
			for (int i = 0; i < Items.Length; i++)
			{
				Brush brush;
				if (i == selectedIndex)
					brush = Items[i].Enabled ? selectedEnabledBrush : selectedDisabledBrush;
				else
					brush = Items[i].Enabled ? enabledBrush : disabledBrush;

				gfx.DrawString(Items[i].Text, font, brush, x - sizes[i].Width / 2, y);
				y += sizes[i].Height;
			}
		}
	}
}

