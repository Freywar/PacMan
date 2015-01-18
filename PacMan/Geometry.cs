using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using System.Drawing;

namespace PacMan
{
	class Geometry
	{
		public static double Distance(double x1, double y1, double x2, double y2)
		{
			x1 -= x2;
			y1 -= y2;
			return Math.Sqrt(x1 * x1 + y1 * y1);
		}

		public static Vector3d FromSpheric(double alpha, double beta, double r)
		{
			return new Vector3d(Math.Cos(alpha) * Math.Cos(beta) * r, Math.Sin(alpha) * r, Math.Cos(alpha) * Math.Sin(beta) * r);
		}


	}
}
