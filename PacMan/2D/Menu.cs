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
	/// <summary>
	/// Menu.
	/// </summary>
	class Menu : Screen2D
	{
		/// <summary>
		/// Menu item.
		/// </summary>
		public struct Item
		{
			/// <summary>
			/// Text.
			/// </summary>
			public string Text;
			/// <summary>
			/// Enabled.
			/// </summary>
			public bool Enabled;
			/// <summary>
			/// Item constructor.
			/// </summary>
			/// <param name="text">Text.</param>
			/// <param name="enabled">Enabled.</param>
			public Item(string text, bool enabled)
			{
				Text = text;
				Enabled = enabled;
			}
		}

		/// <summary>
		/// Navigation repeat timeout(seconds).
		/// </summary>
		private const double navigationTimeout = 0.3;

		/// <summary>
		/// Index of current menu item.
		/// </summary>
		private int selectedIndex = 0;
		/// <summary>
		/// Current pressed key or null if none pressed.
		/// </summary>
		private Key? pressedKey = null;
		/// <summary>
		/// Time elapsed from last navigation if key is pressed.
		/// </summary>
		private double navigationTimeElapsed = 0;

		/// <summary>
		/// Header strings.
		/// </summary>
		public string[] Header = null;

		/// <summary>
		/// Menu items.
		/// </summary>
		public Item[] Items = null;

		/// <summary>
		/// Initialization.
		/// </summary>
		public void Init()
		{
			pressedKey = null;
			navigationTimeElapsed = 0;
			selectedIndex = 0;
		}

		/// <summary>
		/// Update.
		/// </summary>
		/// <param name="dt">Time elapsed from last call(seconds).</param>
		public void Update(double dt)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not navigate empty menu.");

			if (pressedKey != null)
				navigationTimeElapsed += dt;

			if (navigationTimeElapsed >= navigationTimeout)
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

				navigationTimeElapsed = 0;
			}
		}

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="key">Pressed key.</param>
		/// <returns>Selected index on Enter press or null.</returns>
		public int? KeyDown(Key key)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not navigate empty menu.");

			if (key == Key.Up || key == Key.Down)
			{
				pressedKey = key;
				navigationTimeElapsed = navigationTimeout;
			}

			if (key == Key.Enter && Items[selectedIndex].Enabled)
				return selectedIndex;
			else
				return null;
		}

		/// <summary>
		/// Key release handling.
		/// </summary>
		/// <param name="key">Released key.</param>
		/// <returns>Always null.</returns>
		public int? KeyUp(Key key)
		{
			if (key == pressedKey)
			{
				pressedKey = null;
				navigationTimeElapsed = 0;
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
				totalHeaderHeight += headerSizes[0].Height;
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

