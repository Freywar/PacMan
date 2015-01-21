﻿using System;
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
			AppearAnimation,
			LifeLostAnimation,
			WinAnimation,
			LoseAnimation,
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
		private XmlDocument SaveData = null;

		private double animationTime = 0;
		private const double delay = 0;
		private PacMan.States? savedPacManState = null;

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
		private bool loadConfig()
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
						case "save":
							SaveData = new XmlDocument();
							StringReader stringReader = new StringReader(Encoding.UTF8.GetString(Convert.FromBase64String(node.InnerText)));
							XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
							SaveData.Load(xmlTextReader);
							break;
					}
				foreach (XmlAttribute attr in settings.DocumentElement.Attributes)
					if (attr.Name == "autostart" && attr.Value == "true")
						return true;
				return false;

			}
			else
				throw new Exception("Invalid root element in config file.");
		}

		#endregion

		private void createSaveData()
		{
			SaveData = new XmlDocument();
			XmlNode saveDataRoot = SaveData.CreateElement("save");

			XmlNode map = SaveData.CreateElement("map");

			XmlAttribute name = SaveData.CreateAttribute("name");
			name.Value = CurrentMap.Name;
			map.Attributes.Append(name);

			XmlNode clearedCells = SaveData.CreateElement("clearedCells");

			for (int y = 0; y < CurrentMap.Height; y++)
				for (int x = 0; x < CurrentMap.Height; x++)
					if (CurrentMap[y][x] != CurrentMap.OriginalFields[y][x])
					{
						XmlNode cell = SaveData.CreateElement("cell");

						XmlAttribute cxAttr = SaveData.CreateAttribute("x");
						cxAttr.Value = x.ToString();
						cell.Attributes.Append(cxAttr);

						XmlAttribute cyAttr = SaveData.CreateAttribute("y");
						cyAttr.Value = y.ToString();
						cell.Attributes.Append(cyAttr);

						clearedCells.AppendChild(cell);
					}

			map.AppendChild(clearedCells);

			saveDataRoot.AppendChild(map);


			XmlNode pacman = SaveData.CreateElement("pacman");

			XmlAttribute xAttr = SaveData.CreateAttribute("X");
			xAttr.Value = PacMan.X.ToString();
			pacman.Attributes.Append(xAttr);

			XmlAttribute yAttr = SaveData.CreateAttribute("Y");
			yAttr.Value = PacMan.Y.ToString();
			pacman.Attributes.Append(yAttr);

			XmlAttribute lives = SaveData.CreateAttribute("lives");
			lives.Value = PacMan.Lives.ToString();
			pacman.Attributes.Append(lives);

			XmlAttribute state = SaveData.CreateAttribute("state");
			state.Value = PacMan.State.ToString();
			pacman.Attributes.Append(state);

			XmlAttribute superTime = SaveData.CreateAttribute("superTime");
			superTime.Value = PacMan.SuperTime.ToString();
			pacman.Attributes.Append(superTime);

			XmlAttribute direction = SaveData.CreateAttribute("direction");
			direction.Value = PacMan.Direction.ToString();
			pacman.Attributes.Append(direction);

			saveDataRoot.AppendChild(pacman);


			XmlNode ghosts = SaveData.CreateElement("ghosts");

			foreach (Ghost Ghost in Ghosts)
			{
				XmlNode ghost = SaveData.CreateElement("ghost");

				name = SaveData.CreateAttribute("name");
				name.Value = Ghost.Name;
				ghost.Attributes.Append(name);

				xAttr = SaveData.CreateAttribute("X");
				xAttr.Value = Ghost.X.ToString();
				ghost.Attributes.Append(xAttr);

				yAttr = SaveData.CreateAttribute("Y");
				yAttr.Value = Ghost.Y.ToString();
				ghost.Attributes.Append(yAttr);

				state = SaveData.CreateAttribute("state");
				state.Value = Ghost.State.ToString();
				ghost.Attributes.Append(state);

				direction = SaveData.CreateAttribute("direction");
				direction.Value = Ghost.Direction.ToString();
				ghost.Attributes.Append(direction);

				ghosts.AppendChild(ghost);
			}

			saveDataRoot.AppendChild(ghosts);


			XmlNode score = SaveData.CreateElement("score");

			XmlAttribute value = SaveData.CreateAttribute("value");
			value.Value = Score.ToString();
			score.Attributes.Append(value);

			saveDataRoot.AppendChild(score);

			MainMenu.Items[1].Enabled = true;
			MainMenu.Invalidate();

			SaveData.AppendChild(saveDataRoot);
		}

		private void clearSaveData()
		{
			SaveData = null;
			MainMenu.Items[1].Enabled = false;
			MainMenu.Invalidate();
		}

		private void saveConfigToFile()
		{
			XmlDocument settings = new XmlDocument();
			XmlNode root = settings.CreateElement("game");
			settings.AppendChild(root);

			XmlNode pacmanNode = settings.CreateElement("pacman");

			XmlAttribute speedAttr = settings.CreateAttribute("speed");
			speedAttr.Value = PacMan.Speed.ToString();
			pacmanNode.Attributes.Append(speedAttr);

			root.AppendChild(pacmanNode);

			XmlNode ghostsNode = settings.CreateElement("ghosts");

			foreach (Ghost ghost in Ghosts)
			{
				XmlNode ghostNode = settings.CreateElement("ghost");

				XmlAttribute nameAttr = settings.CreateAttribute("name");
				nameAttr.Value = ghost.Name;
				ghostNode.Attributes.Append(nameAttr);

				XmlAttribute colorAttr = settings.CreateAttribute("color");
				colorAttr.Value = ghost.Color.ToKnownColor().ToString();
				ghostNode.Attributes.Append(colorAttr);

				speedAttr = settings.CreateAttribute("speed");
				speedAttr.Value = ghost.Speed.ToString();
				ghostNode.Attributes.Append(speedAttr);

				XmlAttribute frightenedSpeedAttr = settings.CreateAttribute("frightenedSpeed");
				frightenedSpeedAttr.Value = ghost.FrightenedSpeed.ToString();
				ghostNode.Attributes.Append(frightenedSpeedAttr);

				XmlAttribute eatenSpeedAttr = settings.CreateAttribute("eatenSpeed");
				eatenSpeedAttr.Value = ghost.EatenSpeed.ToString();
				ghostNode.Attributes.Append(eatenSpeedAttr);

				XmlAttribute delayAttr = settings.CreateAttribute("delay");
				delayAttr.Value = ghost.Delay.ToString();
				ghostNode.Attributes.Append(delayAttr);

				ghostsNode.AppendChild(ghostNode);
			}

			root.AppendChild(ghostsNode);


			XmlNode mapsNode = settings.CreateElement("maps");

			foreach (Map map in Maps)
			{
				XmlNode mapNode = settings.CreateElement("map");

				XmlAttribute nameAttr = settings.CreateAttribute("name");
				nameAttr.Value = map.Name;
				mapNode.Attributes.Append(nameAttr);

				XmlAttribute pathAttr = settings.CreateAttribute("path");
				pathAttr.Value = map.Path;
				mapNode.Attributes.Append(pathAttr);

				mapsNode.AppendChild(mapNode);
			}

			root.AppendChild(mapsNode);


			XmlNode powerupNode = settings.CreateElement("powerup");

			XmlAttribute durationAttr = settings.CreateAttribute("duration");
			durationAttr.Value = PowerupDuration.ToString();
			powerupNode.Attributes.Append(durationAttr);

			root.AppendChild(powerupNode);

			if (SaveData != null)
			{
				StringWriter stringWriter = new StringWriter();
				XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);
				SaveData.WriteTo(xmlTextWriter);
				string saveData = stringWriter.ToString();

				XmlNode save = settings.CreateElement("save");

				Convert.ToBase64String(Encoding.UTF8.GetBytes(saveData));
				save.InnerText = Convert.ToBase64String(Encoding.UTF8.GetBytes(saveData));

				root.AppendChild(save);
			}


			settings.Save("config.xml");
		}

		/// <summary>
		/// Initialization on game launch.
		/// </summary>
		public void Init()
		{
			bool autostart = loadConfig();

			MainMenu.Header = new string[1] { "PACMAN" };
			MainMenu.Items = new Menu.Item[3];
			MainMenu.Items[0] = new Menu.Item("Start", true);
			MainMenu.Items[1] = new Menu.Item("Continue", SaveData != null);
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

			if (autostart)
				startGame();
		}

		/// <summary>
		/// Start new game.
		/// </summary>
		private void startGame()
		{
			clearSaveData();

			CurrentMap = Maps[0];
			CurrentMap.Init();
			PacMan.Lives = 3;
			Score = 0;
			PacMan.Init(CurrentMap);
			foreach (Ghost ghost in Ghosts)
				ghost.Init(CurrentMap);
			Camera.Init(CurrentMap, PacMan);

			HUD.Score = Score;
			HUD.Lives = PacMan.Lives;

			State = States.AppearAnimation;
		}

		private void loadGame()
		{
			if (SaveData == null)
				startGame();

			Map savedMap = null;

			foreach (XmlNode node in SaveData.DocumentElement)
			{
				if (node.Name == "map")
				{
					foreach (XmlAttribute attr in node.Attributes)
					{
						if (attr.Name == "name")
						{
							foreach (Map map in Maps)
								if (attr.Value == map.Name)
									savedMap = map;
						}
					}
				}
			}

			if (savedMap == null)
			{
				startGame();
				return;
			}

			CurrentMap = savedMap;
			CurrentMap.Init();
			PacMan.Lives = 3;
			Score = 0;
			PacMan.Init(CurrentMap);
			foreach (Ghost ghost in Ghosts)
				ghost.Init(CurrentMap);
			Camera.Init(CurrentMap, PacMan);

			foreach (XmlNode node in SaveData.DocumentElement.ChildNodes)
			{
				switch (node.Name)
				{
					case "map":
						foreach (XmlNode child in node)
						{
							if (child.Name == "clearedCells")
							{
								foreach (XmlNode cellNode in child)
								{
									if (cellNode.Name == "cell")
									{
										int? x = null;
										int? y = null;
										foreach (XmlAttribute attr in cellNode.Attributes)
										{
											if (attr.Name == "x")
												x = Convert.ToInt32(attr.Value);
											if (attr.Name == "y")
												y = Convert.ToInt32(attr.Value);
										}
										if (x != null & y != null)
											CurrentMap[(int)y][(int)x] = Map.Objects.None;
									}
								}
							}
						}
						break;
					case "pacman":
						foreach (XmlAttribute attr in node.Attributes)
						{
							switch (attr.Name)
							{
								case "X":
									PacMan.X = Convert.ToDouble(attr.Value);
									break;
								case "Y":
									PacMan.Y = Convert.ToDouble(attr.Value);
									break;
								case "lives":
									PacMan.Lives = Convert.ToInt32(attr.Value);
									break;
								case "state":
									{
										switch (attr.Value)
										{
											case "Normal":
												savedPacManState = PacMan.States.Normal;
												break;
											case "Super":
												savedPacManState = PacMan.States.Super;
												break;

										}
									}
									break;
								case "direction":
									{
										switch (attr.Value)
										{
											case "None":
												PacMan.Direction = Creature.Directions.None;
												break;
											case "Up":
												PacMan.Direction = Creature.Directions.Up;
												break;
											case "Down":
												PacMan.Direction = Creature.Directions.Down;
												break;
											case "Left":
												PacMan.Direction = Creature.Directions.Left;
												break;
											case "Right":
												PacMan.Direction = Creature.Directions.Right;
												break;
										}
									}
									break;
								case "superTime":
									PacMan.SuperTime = Convert.ToDouble(attr.Value);
									break;
							}
						}
						break;
					case "ghosts":
						foreach (XmlNode ghostNode in node.ChildNodes)
						{
							string name = null;
							foreach (XmlAttribute attr in ghostNode.Attributes)
								if (attr.Name == "name")
									name = attr.Value;

							Ghost currentGhost = null;
							if (name != null)
								foreach (Ghost ghost in Ghosts)
									if (ghost.Name == name)
										currentGhost = ghost;

							if (currentGhost != null)
								foreach (XmlAttribute attr in ghostNode.Attributes)
								{
									switch (attr.Name)
									{
										case "X":
											currentGhost.X = Convert.ToDouble(attr.Value);
											break;
										case "Y":
											currentGhost.Y = Convert.ToDouble(attr.Value);
											break;
										case "state":
											{
												switch (attr.Value)
												{
													case "Waiting":
														currentGhost.State = Ghost.States.Waiting;
														break;
													case "Normal":
														currentGhost.State = Ghost.States.Normal;
														break;
													case "Frightened":
														currentGhost.State = Ghost.States.Frightened;
														break;
													case "Eaten":
														currentGhost.State = Ghost.States.Eaten;
														break;
												}
											}
											break;
										case "direction":
											{
												switch (attr.Value)
												{
													case "None":
														currentGhost.Direction = Creature.Directions.None;
														break;
													case "Up":
														currentGhost.Direction = Creature.Directions.Up;
														break;
													case "Down":
														currentGhost.Direction = Creature.Directions.Down;
														break;
													case "Left":
														currentGhost.Direction = Creature.Directions.Left;
														break;
													case "Right":
														currentGhost.Direction = Creature.Directions.Right;
														break;
												}
											}
											break;
									}
								}
						}
						break;
					case "score":
						foreach (XmlAttribute attr in node.Attributes)
							if (attr.Name == "value")
								Score = Convert.ToInt32(attr.Value);
						break;
				}
			}

			HUD.Score = Score;
			HUD.Lives = PacMan.Lives;

			State = States.AppearAnimation;
		}

		private void nextMap()
		{
			int currentMapIndex = 0;
			for (int i = 0; i < Maps.Length; i++)
				if (CurrentMap == Maps[i])
					currentMapIndex = i;
			if (currentMapIndex == Maps.Length - 1)
			{
				clearSaveData();
				WonMenu.Header[1] = "Score: " + Score.ToString();
				State = States.WonMenu;
			}
			else
			{
				CurrentMap = Maps[currentMapIndex + 1];
				CurrentMap.Init();
				PacMan.Init(CurrentMap);
				foreach (Ghost ghost in Ghosts)
					ghost.Init(CurrentMap);
				Camera.Init(CurrentMap, PacMan);
				State = States.AppearAnimation;
			}
		}

		/// <summary>
		/// Restart level after death.
		/// </summary>
		private void restartLevel()
		{
			PacMan.Init(CurrentMap);
			foreach (Ghost ghost in Ghosts)
				ghost.Init(CurrentMap);

			State = States.AppearAnimation;
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

				case States.AppearAnimation:
					if (CurrentMap.State == Map.States.None)
						CurrentMap.State = Map.States.AppearAnimation;

					CurrentMap.Update(dt);
					if (CurrentMap.State == Map.States.Normal)
					{
						PacMan.State = PacMan.States.AppearAnimation;

						PacMan.Update(dt, CurrentMap);
						if (PacMan.State == PacMan.States.Normal)
						{
							if (savedPacManState != null)
							{
								PacMan.State = (PacMan.States)savedPacManState;
								savedPacManState = null;
							}
							foreach (Ghost ghost in Ghosts)
								if (ghost.State == Ghost.States.None)
									ghost.State = Ghost.States.Waiting;
							State = States.Playing;
						}
					}
					break;

				case States.Playing:

					Point? pacManVisitedCell = PacMan.Update(dt, CurrentMap);
					foreach (Ghost ghost in Ghosts)
						ghost.Update(dt, CurrentMap, PacMan);
					Camera.Update(dt, CurrentMap, PacMan);

					if (pacManVisitedCell != null)
					{
						Point visitedCell = (Point)pacManVisitedCell;
						if (CurrentMap[visitedCell.Y][visitedCell.X] == Map.Objects.Point)
							Score += 10;
						if (CurrentMap[visitedCell.Y][visitedCell.X] == Map.Objects.Powerup)
						{
							Score += 100;
							PacMan.State = PacMan.States.Super;
							PacMan.SuperTime += PowerupDuration;
							foreach (Ghost ghost in Ghosts)
								ghost.State = Ghost.States.Frightened;
						}
						CurrentMap[visitedCell.Y][visitedCell.X] = Map.Objects.None;
					}
					if (PacMan.State == PacMan.States.Super)
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
									State = PacMan.Lives != 0 ? States.LifeLostAnimation : States.LoseAnimation;
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
						State = States.WinAnimation;


					HUD.Score = Score;
					HUD.Lives = PacMan.Lives;
					break;


				case States.LifeLostAnimation:
					if (PacMan.State == PacMan.States.Normal)
						PacMan.State = PacMan.States.DisappearAnimation;


					PacMan.Update(dt, CurrentMap);
					if (PacMan.State == PacMan.States.None)
					{
						foreach (Ghost ghost in Ghosts)
							ghost.State = Ghost.States.DisappearAnimation;
					}

					bool allGhostsDisappeard = true;
					foreach (Ghost ghost in Ghosts)
					{
						ghost.Update(dt, CurrentMap, PacMan);
						allGhostsDisappeard = allGhostsDisappeard && ghost.State == Ghost.States.None;
					}

					if (allGhostsDisappeard)
						restartLevel();

					break;

				case States.WinAnimation:
					if (CurrentMap.State == Map.States.Normal)
						CurrentMap.State = Map.States.DisappearAnimation;

					CurrentMap.Update(dt);

					allGhostsDisappeard = true;
					foreach (Ghost ghost in Ghosts)
					{
						if (ghost.State != Ghost.States.DisappearAnimation && ghost.State != Ghost.States.None)
							ghost.State = Ghost.States.DisappearAnimation;
						ghost.Update(dt, CurrentMap, PacMan);
						allGhostsDisappeard = allGhostsDisappeard && ghost.State == Ghost.States.None;
					}

					if (allGhostsDisappeard && CurrentMap.State == Map.States.None)
						nextMap();

					break;

				case States.LoseAnimation:
					if (PacMan.State == PacMan.States.Normal)
						PacMan.State = PacMan.States.DisappearAnimation;

					PacMan.Update(dt, CurrentMap);
					if (PacMan.State == PacMan.States.None)
					{
						foreach (Ghost ghost in Ghosts)
							ghost.State = Ghost.States.DisappearAnimation;
						CurrentMap.State = Map.States.DisappearAnimation;
					}

					CurrentMap.Update(dt);

					allGhostsDisappeard = true;
					foreach (Ghost ghost in Ghosts)
					{
						ghost.Update(dt, CurrentMap, PacMan);
						allGhostsDisappeard = allGhostsDisappeard && ghost.State == Ghost.States.None;
					}

					if (allGhostsDisappeard && CurrentMap.State == Map.States.None)
					{
						clearSaveData();
						State = States.LostMenu;
					}
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
						loadGame();
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
					{
						createSaveData();
						State = States.MainMenu;
					}
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

				case States.AppearAnimation:
				case States.Playing:
				case States.WinAnimation:
				case States.LifeLostAnimation:
				case States.LoseAnimation:


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

					if (CurrentMap.State == Map.States.Normal)
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

		public void Exit()
		{
			if (State == States.Playing)
				createSaveData();
			saveConfigToFile();
		}
	}
}
