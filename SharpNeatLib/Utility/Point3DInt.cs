namespace SharpNeat.Utility
{
    public struct Point3DInt
    {
        public Point3DInt(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; set; }

        public int Y { get; set; }
        
        public int Z { get; set; }

        public override bool Equals(object obj)
        {
            if (!(obj is Point3DInt))
                return false;

            var point3DInt = (Point3DInt) obj;

            return point3DInt.X == X && point3DInt.Y == Y;
        }
        
        public override int GetHashCode()
        {
            return (X + (17*Y) + (32*Z));
        }
    }
}