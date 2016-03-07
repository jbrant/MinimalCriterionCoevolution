namespace SharpNeat.Phenomes.Mazes
{
    public class MazeWall
    {
        #region Constructors

        public MazeWall()
        {
        }

        public MazeWall(int xStart, int yStart, int xEnd, int yEnd)
        {
            StartPoint = new Point(xStart, yStart);
            EndPoint = new Point(xEnd, yEnd);
        }

        #endregion

        #region Properties

        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }

        #endregion
    }

    public struct Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}