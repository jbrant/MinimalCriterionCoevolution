namespace SharpNeat.Utility
{
    public struct Point3DDouble
    {
        public double X;
        public double Y;
        public double Z;

        public Point3DDouble(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(Point3DDouble other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X.GetHashCode();
                hashCode = (hashCode * 397) ^ Y.GetHashCode();
                hashCode = (hashCode * 397) ^ Z.GetHashCode();
                return hashCode;
            }
        }
    }
}