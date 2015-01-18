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
			if (pressedKey != null)
				pressedKeyTime += dt;
		}

		public int? Control(KeyboardDevice keyboard)
		{
			if (Items == null || Items.Length == 0)
				throw new NotSupportedException("Can not navigate empty menu.");

			if (keyboard[Key.Up] && !keyboard[Key.Down] && (pressedKey != Key.Up || pressedKeyTime > 0.5))
			{
				if (selectedIndex > 0)
				{
					selectedIndex--;
					textureIsValid = false;
				}

				pressedKey = Key.Up;
				pressedKeyTime = 0;
			}

			if (keyboard[Key.Down] && !keyboard[Key.Up] && (pressedKey != Key.Down || pressedKeyTime > 0.5))
			{
				if (selectedIndex < Items.Length - 1)
				{
					selectedIndex++;
					textureIsValid = false;
				}
				pressedKey = Key.Down;
				pressedKeyTime = 0;
			}

			if (pressedKey != null && !keyboard[(Key)pressedKey])
			{
				pressedKey = null;
				pressedKeyTime = 0;
			}

			if (keyboard[Key.Enter] && Items[selectedIndex].Enabled)
				return selectedIndex;
			else
				return null;
		}

		protected override void render2D(Graphics gfx)
		{
			Font font = new Font(new FontFamily("Tahoma"), 36, FontStyle.Bold);
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

			float x = Width / 2, y = Height / 2 - totalHeight / 2;
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

