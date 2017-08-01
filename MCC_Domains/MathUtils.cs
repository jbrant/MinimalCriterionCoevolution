using System;

namespace SharpNeat.Domains
{
    public static class MathUtils
    {
        public static double toRadians(double angleInDegrees)
        {
            return (Math.PI/180)*angleInDegrees;
        }

        public static double toDegrees(double angleInRadians)
        {
            return (180/Math.PI)*angleInRadians;
        }
    }
}