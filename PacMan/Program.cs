using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace PacMan
{

	class Application
	{
		private static Game Game;
		private static GameWindow Window;

		public static void OnWindowLoad(Object sender, EventArgs e)
		{
			Game = new Game();
			Game.Init();
		}

		public static void OnWindowResize(Object sender, EventArgs e)
		{
			GL.Viewport(0, 0, Window.Width, Window.Height);
			Game.Width = Window.Width;
			Game.Height = Window.Height;
		}

		public static void OnWindowUpdate(Object sender, FrameEventArgs e)
		{
			Game.Update(e.Time);
			Game.Control(Window.Keyboard);
		}

		public static void OnWindowRender(Object sender, FrameEventArgs e)
		{
			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

			

			Game.Render();


			Window.SwapBuffers();
		}



		[STAThread]
		public static void Main()
		{
			Window = new GameWindow();
			Game = new Game();
			Window.Load += OnWindowLoad;
			Window.Resize += OnWindowResize;
			Window.UpdateFrame += OnWindowUpdate;
			Window.RenderFrame += OnWindowRender;
			Window.Run(60.0);
		}
	}
}