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
	/// Main game class.
	/// </summary>
	class Game
	{
		/// <summary>
		/// Game states.
		/// </summary>
		private enum States
		{
			/// <summary>
			/// Logo screen.
			/// </summary>
			Starting,
			/// <summary>
			/// Game.
			/// </summary>
			Playing,
			/// <summary>
			/// Win screen.
			/// </summary>
			Won,
			/// <summary>
			/// Lose screen.
			/// </summary>
			Lose
		}

		/// <summary>
		/// Game state.
		/// </summary>
		private States State = States.Starting;
		/// <summary>
		/// Camera.
		/// </summary>
		private Camera Camera = new Camera();
		/// <summary>
		/// Pacman.
		/// </summary>
		private PacMan PacMan = new PacMan();
		/// <summary>
		/// Ghosts.
		/// </summary>
		private Ghost[] Ghosts = null;
		/// <summary>
		/// Maps.
		/// </summary>
		private Map[] Maps = null;
		/// <summary>
		/// Current map.
		/// </summary>
		private Map CurrentMap = null;
		private HUD HUD = new HUD();

		/// <summary>
		/// Powerup duration(seconds).
		/// </summary>
		private double PowerupDuration = 30;

		private Bitmap text_bmp;
		private int text_texture = -1;

		private int Width_v = 0;
		private int Height_v = 0;

		/// <summary>
		/// Window width(pixels).
		/// </summary>
		public int Width
		{
			get { return Width_v; }
			set
			{
				text_texture = -1;
				HUD.Width = value;
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
				text_texture = -1;
				HUD.Height = value;
				Height_v = value;
			}
		}
		/// <summary>
		/// Score.
		/// </summary>
		private int Score = 0;

		#region Load config from file.

		private void loadPacmanConfig(XmlNode node)
		{
			foreach (XmlAttribute attr in node.Attributes)
			{
				switch (attr.Name)
				{
					case "speed":
						PacMan.Speed = Convert.ToInt32(attr.Value);
						break;
				}
			}
		}

		private void loadGhostsConfig(XmlNode node)
		{
			int count = 0;
			foreach (XmlNode ghostNode in node.ChildNodes)
				if (ghostNode.Name == "ghost")
					count++;

			Ghosts = new Ghost[count];
			int j = 0;
			foreach (XmlNode ghostNode in node.ChildNodes)
				if (ghostNode.Name == "ghost")
				{
					Ghosts[j] = new Ghost();

					foreach (XmlAttribute attr in ghostNode.Attributes)
					{
						switch (attr.Name)
						{
							case "name":
								Ghosts[j].Name = attr.Value;
								break;
							case "color":
								Ghosts[j].Color = Color.FromName(attr.Value);
								break;
							case "speed":
								Ghosts[j].Speed = Convert.ToDouble(attr.Value);
								break;
							case "frightenedSpeed":
								Ghosts[j].FrightenedSpeed = Convert.ToDouble(attr.Value);
								break;
							case "eatenSpeed":
								Ghosts[j].EatenSpeed = Convert.ToDouble(attr.Value);
								break;
							case "delay":
								Ghosts[j].Delay = Convert.ToDouble(attr.Value);
								break;
						}
					}
					j++;
				}
		}

		private void loadMapsConfig(XmlNode node)
		{
			int count = 0;
			foreach (XmlNode mapNode in node.ChildNodes)
				if (mapNode.Name == "map")
					count++;

			Maps = new Map[count];

			int i = 0;
			foreach (XmlNode mapNode in node.ChildNodes)
				if (mapNode.Name == "map")
				{
					Maps[i] = new Map();

					foreach (XmlAttribute attr in mapNode.Attributes)
					{
						switch (attr.Name)
						{
							case "name":
								Maps[i].Name = attr.Value;
								break;
							case "path":
								Maps[i].Path = attr.Value;
								break;
						}
					}
					i++;
				}
		}

		/// <summary>
		/// Load config from file.
		/// </summary>
		private void loadConfig()
		{
			XmlDocument settings = new XmlDocument();
			settings.Load("config.xml");

			if (settings.DocumentElement.Name == "game")
			{
				foreach (XmlNode node in settings.DocumentElement.ChildNodes)
					switch (node.Name)
					{
						case "pacman":
							loadPacmanConfig(node);
							break;
						case "ghosts":
							loadGhostsConfig(node);
							break;
						case "maps":
							loadMapsConfig(node);
							break;
						case "powerup":
							foreach (XmlAttribute attr in node.Attributes)
								if (attr.Name == "duration")
									PowerupDuration = Convert.ToDouble(attr.Value);
							break;
					}
			}
			else
				throw new Exception("Invalid root element in config file.");
		}

		#endregion

		/// <summary>
		/// Initialization on game launch.
		/// </summary>
		public void Init()
		{
			loadConfig();

			float[] light_diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
			float[] light_position = { 0.0f, 0.0f, 2.0f, 0.0f };
			float[] light_ambient = { 0.3f, 0.3f, 0.3f, 1.0f };

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			GL.ShadeModel(ShadingModel.Smooth);

			GL.Light(LightName.Light0, LightParameter.Position, light_position);
			GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
			GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);

			State = States.Starting;
		}

		/// <summary>
		/// Start new game.
		/// </summary>
		private void startGame()
		{
			CurrentMap = Maps[0];
			CurrentMap.Init();
			PacMan.Lives = 3;
			PacMan.Init(CurrentMap);
			foreach (Ghost ghost in Ghosts)
				ghost.Init(CurrentMap);
			Camera.Init(CurrentMap, PacMan);

			State = States.Playing;
		}

		/// <summary>
		/// Restart level after death.
		/// </summary>
		private void restartLevel()
		{
			PacMan.Init(CurrentMap);
			foreach (Ghost ghost in Ghosts)
				ghost.Init(CurrentMap);

			State = States.Playing;
		}

		/// <summary>
		/// Update game properties.
		/// </summary>
		/// <param name="dt">Time elapsed from last call(seconds).</param>
		public void Update(double dt)
		{
			if (State == States.Playing)
			{
				Point pacManVisitedCell = PacMan.Update(dt, CurrentMap);
				foreach (Ghost ghost in Ghosts)
					ghost.Update(dt, CurrentMap, PacMan);
				Camera.Update(dt, CurrentMap, PacMan);

				if (pacManVisitedCell != Point.Empty)
				{
					if (CurrentMap[pacManVisitedCell.Y][pacManVisitedCell.X] == Map.Objects.Point)
						Score += 10;
					if (CurrentMap[pacManVisitedCell.Y][pacManVisitedCell.X] == Map.Objects.Powerup)
					{
						Score += 100;
						PacMan.State = PacMan.States.Super;
						PacMan.SuperTime += PowerupDuration;
						foreach (Ghost ghost in Ghosts)
							ghost.State = Ghost.States.Frightened;
					}
					CurrentMap[pacManVisitedCell.Y][pacManVisitedCell.X] = Map.Objects.None;
				}

				PacMan.SuperTime -= dt;
				if (PacMan.State == PacMan.States.Super && PacMan.SuperTime <= 0)
				{
					PacMan.State = PacMan.States.Normal;
					PacMan.SuperTime = 0;
					foreach (Ghost ghost in Ghosts)
						if (ghost.State == Ghost.States.Frightened)
							ghost.State = Ghost.States.Normal;
				}

				foreach (Ghost ghost in Ghosts)
				{
					switch (ghost.State)
					{
						case Ghost.States.Normal:
							if (Geometry.Distance(PacMan.X, PacMan.Y, ghost.X, ghost.Y) < 1)
							{
								PacMan.Lives--;
								if (PacMan.Lives == 0)
									State = States.Lose;
								else
									restartLevel();
							}
							break;
						case Ghost.States.Frightened:
							if (Geometry.Distance(PacMan.X, PacMan.Y, ghost.X, ghost.Y) < 1)
							{
								Score += 100;
								ghost.State = Ghost.States.Eaten;
							}
							break;
						case Ghost.States.Eaten:
							if (Geometry.Distance(ghost.X, ghost.Y, CurrentMap.GhostStart.X, CurrentMap.GhostStart.Y) < 0.1)
								ghost.State = Ghost.States.Waiting;
							break;
						default:
							break;
					}
				}

				if (CurrentMap.PointsCount == 0)
					State = States.Won;

					HUD.Score = Score;
					HUD.Lives = PacMan.Lives;
			}
		}

		/// <summary>
		/// User input handling.
		/// </summary>
		/// <param name="keyboard">Pressed keys.</param>
		public void Control(KeyboardDevice keyboard)
		{
			switch (State)
			{
				case States.Starting:
					if (keyboard[Key.Enter])
						startGame();
					break;
				case States.Playing:
					PacMan.Control(keyboard);
					Camera.Control(keyboard);
					break;

				case States.Lose:
					if (keyboard[Key.Enter])
						startGame();
					break;

				case States.Won:
					if (keyboard[Key.Enter])
						startGame();
					break;
			}
		}

		/// <summary>
		/// Render.
		/// </summary>
		public void Render()
		{
			switch (State)
			{
				case States.Starting:
					GL.MatrixMode(MatrixMode.Projection);
					GL.LoadIdentity();
					GL.Ortho(0, Width, 0, Height, -1, 1);

					GL.MatrixMode(MatrixMode.Modelview);
					GL.LoadIdentity();
					if (text_texture == -1)
					{
						text_bmp = new Bitmap(Width, Height);

						text_texture = GL.GenTexture();
						GL.BindTexture(TextureTarget.Texture2D, text_texture);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
						GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, text_bmp.Width, text_bmp.Height, 0,
							 PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // just allocate memory, so we can update efficiently using TexSubImage2D
					}

					using (Graphics gfx = Graphics.FromImage(text_bmp))
					{
						gfx.Clear(Color.Transparent);
						gfx.DrawString("Press enter", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold), new SolidBrush(Color.Yellow),

						Width / 2 - gfx.MeasureString("Press enter", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Width / 2,
						Height / 2 - gfx.MeasureString("Press enter", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Height / 2);

					}

					System.Drawing.Imaging.BitmapData data = text_bmp.LockBits(new Rectangle(0, 0, text_bmp.Width, text_bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
						 PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
					text_bmp.UnlockBits(data);

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

					break;

				case States.Playing:



					GL.Enable(EnableCap.Lighting);
					GL.Enable(EnableCap.Light0);
					GL.Enable(EnableCap.DepthTest);
					GL.Enable(EnableCap.ColorMaterial);
					GL.Enable(EnableCap.CullFace);

					GL.MatrixMode(MatrixMode.Projection);
					GL.LoadIdentity();
					Matrix4 proj = Matrix4.CreatePerspectiveFieldOfView((float)(60 * Math.PI / 180), (float)Width / Height, 0.1f, 100f);
					GL.MultMatrix(ref proj);

					GL.MatrixMode(MatrixMode.Modelview);
					GL.LoadIdentity();

					Camera.Render();
					CurrentMap.Render();

					//GL.Translate(-CurrentMap.Width / 2, 0, -CurrentMap.Height / 2);

					PacMan.Render();
					for (int i = 0; i < Ghosts.Length; i++)
						Ghosts[i].Render();




					GL.Disable(EnableCap.Lighting);
					GL.Disable(EnableCap.Light0);
					GL.Disable(EnableCap.DepthTest);
					GL.Disable(EnableCap.ColorMaterial);
					GL.Disable(EnableCap.CullFace);

					HUD.Render();

					break;


				case States.Lose:
					GL.MatrixMode(MatrixMode.Projection);
					GL.LoadIdentity();
					GL.Ortho(0, Width, 0, Height, -1, 1);
					GL.MatrixMode(MatrixMode.Modelview);
					GL.LoadIdentity();

					if (text_texture == -1)
					{
						text_bmp = new Bitmap(Width, Height);

						text_texture = GL.GenTexture();
						GL.BindTexture(TextureTarget.Texture2D, text_texture);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
						GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, text_bmp.Width, text_bmp.Height, 0,
							 PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // just allocate memory, so we can update efficiently using TexSubImage2D
					}

					using (Graphics gfx = Graphics.FromImage(text_bmp))
					{
						gfx.Clear(Color.Transparent);
						gfx.DrawString("Yuo Lose", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold), new SolidBrush(Color.Yellow),

						Width / 2 - gfx.MeasureString("Yuo Lose", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Width / 2,
						Height / 2 - gfx.MeasureString("Yuo Lose", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Height / 2);

					}

					System.Drawing.Imaging.BitmapData data_l = text_bmp.LockBits(new Rectangle(0, 0, text_bmp.Width, text_bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
						 PixelFormat.Bgra, PixelType.UnsignedByte, data_l.Scan0);
					text_bmp.UnlockBits(data_l);

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

					break;

				case States.Won:
					GL.MatrixMode(MatrixMode.Projection);
					GL.LoadIdentity();
					GL.Ortho(0, Width, 0, Height, -1, 1);
					GL.MatrixMode(MatrixMode.Modelview);
					GL.LoadIdentity();

					if (text_texture == -1)
					{
						text_bmp = new Bitmap(Width, Height);

						text_texture = GL.GenTexture();
						GL.BindTexture(TextureTarget.Texture2D, text_texture);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
						GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
						GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, text_bmp.Width, text_bmp.Height, 0,
							 PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // just allocate memory, so we can update efficiently using TexSubImage2D
					}

					using (Graphics gfx = Graphics.FromImage(text_bmp))
					{
						gfx.Clear(Color.Transparent);
						gfx.DrawString("Yoy vin", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold), new SolidBrush(Color.Yellow),

						Width / 2 - gfx.MeasureString("Yoy vin", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Width / 2,
						Height / 2 - gfx.MeasureString("Yoy vin", new Font(new FontFamily("Tahoma"), 32, FontStyle.Bold)).Height / 2);

					}

					System.Drawing.Imaging.BitmapData data_w = text_bmp.LockBits(new Rectangle(0, 0, text_bmp.Width, text_bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
					GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Width, Height, 0,
						 PixelFormat.Bgra, PixelType.UnsignedByte, data_w.Scan0);
					text_bmp.UnlockBits(data_w);

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

					break;
			}

		}
	}
}
