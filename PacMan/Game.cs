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
			/// Main menu.
			/// </summary>
			MainMenu,
			/// <summary>
			/// Game.
			/// </summary>
			Playing,
			/// <summary>
			/// Pause menu.
			/// </summary>
			PauseMenu,
			/// <summary>
			/// Win screen.
			/// </summary>
			WonMenu,
			/// <summary>
			/// Lose screen.
			/// </summary>
			LostMenu
		}

		/// <summary>
		/// Game state.
		/// </summary>
		private States State = States.MainMenu;
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
		private Menu MainMenu = new Menu();
		private Menu PauseMenu = new Menu();
		private Menu WonMenu = new Menu();
		private Menu LostMenu = new Menu();

		/// <summary>
		/// Powerup duration(seconds).
		/// </summary>
		private double PowerupDuration = 30;

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
				HUD.Width = value;
				MainMenu.Width = value;
				PauseMenu.Width = value;
				WonMenu.Width = value;
				LostMenu.Width = value;
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
				HUD.Height = value;
				MainMenu.Height = value;
				PauseMenu.Height = value;
				WonMenu.Height = value;
				LostMenu.Height = value;
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

			MainMenu.Header = new string[1] { "PACMAN" };
			MainMenu.Items = new Menu.Item[3];
			MainMenu.Items[0] = new Menu.Item("Start", true);
			MainMenu.Items[1] = new Menu.Item("Continue", false);
			MainMenu.Items[2] = new Menu.Item("Exit", true);

			PauseMenu.Header = new string[1] { "Pause" };
			PauseMenu.Items = new Menu.Item[2];
			PauseMenu.Items[0] = new Menu.Item("Continue", true);
			PauseMenu.Items[1] = new Menu.Item("Main menu", true);

			WonMenu.Header = new string[2] { "You won", "Score: " + Score.ToString() };
			WonMenu.Items = new Menu.Item[1];
			WonMenu.Items[0] = new Menu.Item("Continue", true);

			LostMenu.Header = new string[2] { "Game over", "Score: " + Score.ToString() };
			LostMenu.Items = new Menu.Item[2];
			LostMenu.Items[0] = new Menu.Item("Restart", true);
			LostMenu.Items[1] = new Menu.Item("Main menu", true);

			float[] light_diffuse = { 1.0f, 1.0f, 1.0f, 1.0f };
			float[] light_position = { 0.0f, 0.0f, 2.0f, 0.0f };
			float[] light_ambient = { 0.3f, 0.3f, 0.3f, 1.0f };

			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			GL.ShadeModel(ShadingModel.Smooth);

			GL.Light(LightName.Light0, LightParameter.Position, light_position);
			GL.Light(LightName.Light0, LightParameter.Ambient, light_ambient);
			GL.Light(LightName.Light0, LightParameter.Diffuse, light_diffuse);


			MainMenu.Init();
			State = States.MainMenu;
		}

		/// <summary>
		/// Start new game.
		/// </summary>
		private void startGame()
		{
			CurrentMap = Maps[0];
			CurrentMap.Init();
			PacMan.Lives = 3;
			Score = 0;
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
		/// <returns>Exit flag.</returns>
		public bool Update(double dt)
		{
			switch (State)
			{
				case States.MainMenu:
					MainMenu.Update(dt);
					break;

				case States.Playing:
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
								if (Utils.Distance(PacMan.X, PacMan.Y, ghost.X, ghost.Y) < 1)
								{
									PacMan.Lives--;
									if (PacMan.Lives == 0)
										State = States.LostMenu;
									else
										restartLevel();
								}
								break;
							case Ghost.States.Frightened:
								if (Utils.Distance(PacMan.X, PacMan.Y, ghost.X, ghost.Y) < 1)
								{
									Score += 100;
									ghost.State = Ghost.States.Eaten;
								}
								break;
							case Ghost.States.Eaten:
								if (Utils.Distance(ghost.X, ghost.Y, CurrentMap.GhostStart.X, CurrentMap.GhostStart.Y) < 0.1)
									ghost.State = Ghost.States.Waiting;
								break;
							default:
								break;
						}
					}

					if (CurrentMap.PointsCount == 0)
						State = States.WonMenu;

					HUD.Score = Score;
					HUD.Lives = PacMan.Lives;
					break;

				case States.PauseMenu:
					PauseMenu.Update(dt);
					break;

				case States.WonMenu:
					WonMenu.Update(dt);
					break;

				case States.LostMenu:
					LostMenu.Update(dt);
					break;
			}
			return false;
		}

		/// <summary>
		/// Key press handling.
		/// </summary>
		/// <param name="key">Pressed key.</param>
		/// <returns>Exit flag.</returns>
		public bool KeyDown(Key key)
		{
			int? selectedIndex;
			switch (State)
			{
				case States.MainMenu:
					selectedIndex = MainMenu.KeyDown(key);
					if (selectedIndex == 0)
						startGame();
					if (selectedIndex == 1)
						throw new NotImplementedException();
					if (selectedIndex == 2)
						return true;
					if (key == Key.Escape)
						return true;
					break;
				case States.Playing:
					PacMan.KeyDown(key);
					Camera.KeyDown(key);
					if (key == Key.Escape)
						State = States.PauseMenu;
					break;
				case States.PauseMenu:
					selectedIndex = PauseMenu.KeyDown(key);
					if (selectedIndex == 0)
						State = States.Playing;
					if (selectedIndex == 1)
						State = States.MainMenu;
					if (key == Key.Escape)
						State = States.Playing;
					break;
				case States.WonMenu:
					selectedIndex = WonMenu.KeyDown(key);
					if (selectedIndex == 0)
						State = States.MainMenu;
					if (key == Key.Escape)
						State = States.MainMenu;
					break;
				case States.LostMenu:
					selectedIndex = LostMenu.KeyDown(key);
					if (selectedIndex == 0)
						startGame();
					if (selectedIndex == 1)
						State = States.MainMenu;
					if (key == Key.Escape)
						State = States.MainMenu;
					break;
			}
			return false;
		}

		/// <summary>
		/// Key release handling.
		/// </summary>
		/// <param name="key">Released key.</param>
		/// <returns>Exit flag.</returns>
		public bool KeyUp(Key key)
		{
			int? selectedIndex;
			switch (State)
			{
				case States.MainMenu:
					selectedIndex = MainMenu.KeyUp(key);
					break;
				case States.Playing:
					PacMan.KeyUp(key);
					Camera.KeyUp(key);
					break;
				case States.PauseMenu:
					selectedIndex = PauseMenu.KeyUp(key);
					break;
				case States.WonMenu:
					break;
				case States.LostMenu:
					break;
			}
			return false;
		}

		/// <summary>
		/// Render.
		/// </summary>
		public void Render()
		{
			switch (State)
			{
				case States.MainMenu:
					MainMenu.Render();
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
				case States.PauseMenu:
					PauseMenu.Render();
					break;

				case States.WonMenu:
					WonMenu.Render();
					break;

				case States.LostMenu:
					LostMenu.Render();
					break;
			}
		}
	}
}
