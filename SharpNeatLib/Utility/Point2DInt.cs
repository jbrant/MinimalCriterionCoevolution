using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Disable comment warnings for trivial class.
#pragma warning disable 1591

namespace SharpNeat.Utility
{
    public struct Point2DInt
    {
        int _x;
        int _y;

        public Point2DInt(int x, int y)
        {
            _x = x;
            _y = y;
        }

        public int X
        {
            get { return _x; }
            set { _x = value; }
        }

        public int Y
        {
            get { return _y; }
            set { _y = value; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Point2DInt))
                return false;

            Point2DInt point2DInt = (Point2DInt) obj;

            return point2DInt.X == X && point2DInt.Y == Y;
        }
    }
}
