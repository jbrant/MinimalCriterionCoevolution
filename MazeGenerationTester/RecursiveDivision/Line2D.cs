using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MazeGenerationTester.RecursiveDivision
{
    public class Line2D
    {
        public Line2D()
        {
        }

        public Line2D(int xStart, int yStart, int xEnd, int yEnd)
        {
            StartPoint = new Point(xStart, yStart);
            EndPoint = new Point(xEnd, yEnd);
        }

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
    }
}
