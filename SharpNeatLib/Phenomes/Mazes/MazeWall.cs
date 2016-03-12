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
            StartMazePoint = new MazePoint(xStart, yStart);
            EndMazePoint = new MazePoint(xEnd, yEnd);
        }

        #endregion

        #region Properties

        public MazePoint StartMazePoint { get; set; }
        public MazePoint EndMazePoint { get; set; }

        #endregion
    }

    public struct MazePoint
    {
        public MazePoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}