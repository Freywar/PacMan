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
	/// Base class for all 2D graphics.
	/// </summary>
	abstract class Screen2D
	{
		private Bitmap texture_v = null;
		private int Width_v = 0;
		private int Height_v = 0;

		/// <summary>
		/// Texture.
		/// </summary>
		private Bitmap texture
		{
			get
			{
				if (texture_v == null)
				{
					texture_v = new Bitmap(Width, Height);
					textureId = GL.GenTexture();
					GL.BindTexture(TextureTarget.Texture2D, (int)textureId);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
					GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, texture_v.Width, texture_v.Height, 0,
						 PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);
				}
				return texture_v;
			}
			set
			{
				textureIsValid = false;
				if (texture_v != null && value == null)
				{
					GL.DeleteTexture((int)textureId);
					textureId = null;
				}
				texture_v = value;
			}
		}
		/// <summary>
		/// OpenGL texture id.
		/// </summary>
		private int? textureId = null;
		/// <summary>
		/// Texture needs OpenGL rendering only.
		/// </summary>
		protected bool textureIsValid = false;

		/// <summary>
		/// Window width(pixels).
		/// </summary>
		public int Width
		{
			get { return Width_v; }
			set
			{
				texture = null;
				Width_v = value;
			}
		}
		/// <summary>
		/// Window height(pixels).
		/// </summary>
		public int Height
		{
			get { return Height_v; }
			set
			{
				texture = null;
				Height_v = value;
			}
		}

		/// <summary>
		/// Render on 2D canvas.
		/// </summary>
		/// <param name="gfx">Canvas.</param>
		abstract protected void render2D(Graphics gfx);

		/// <summary>
		/// Render.
		/// </summary>
		virtual public void Render()
		{
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(0, Width, 0, Height, -1, 1);

			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			if (!textureIsValid)
			{
				Graphics gfx = Graphics.FromImage(texture);
				gfx.Clear(Color.Transparent);

				render2D(gfx);

				GL.BindTexture(TextureTarget.Texture2D, (int)textureId);
				System.Drawing.Imaging.BitmapData data = texture.LockBits(new Rectangle(0, 0, texture.Width, texture.Height),
					System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
					 PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
				texture.UnlockBits(data);

				textureIsValid = true;
			}
			else
				GL.BindTexture(TextureTarget.Texture2D, (int)textureId);

			GL.Enable(EnableCap.Texture2D);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

			GL.Begin(PrimitiveType.Quads);

			GL.TexCoord2(0f, 1f); GL.Vertex2(0f, 0f);
			GL.TexCoord2(1f, 1f); GL.Vertex2(Width, 0f);
			GL.TexCoord2(1f, 0f); GL.Vertex2(Width, Height);
			GL.TexCoord2(0f, 0f); GL.Vertex2(0f, Height);
			GL.End();

			GL.Disable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
	}
}
