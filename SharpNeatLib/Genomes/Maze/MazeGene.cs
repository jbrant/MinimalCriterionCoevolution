namespace SharpNeat.Genomes.Maze
{
    public class MazeGene
    {
        public MazeGene(double wallLocation, double passageLocation, bool orientationSeed)
        {
            WallLocation = wallLocation;
            PassageLocation = passageLocation;
            OrientationSeed = orientationSeed;
        }

        public double WallLocation { get; set; }
        public double PassageLocation { get; set; }
        public bool OrientationSeed { get; }
    }
}