using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
	}
}
