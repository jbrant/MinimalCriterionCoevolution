#region

using System;
using SharpNeat.Core;

#endregion

namespace SharpNeat.Genomes.Maze
{
    public class MazeGenome : IGenome<MazeGenome>
    {
        #region Maze Genome Constructors

        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration, double wallStartLocation, double passageStartLocation)
        {
            // Set the genome factory
            _genomeFactory = genomeFactory;

            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Set the maze parameters (wall and passage start location)
            WallStartLocation = wallStartLocation;
            PassageStartLocation = passageStartLocation;

            // Create new evaluation info with no fitness history
            EvaluationInfo = new EvaluationInfo(0);
        }

        public MazeGenome(MazeGenome copyFrom, uint id, uint birthGeneration)
        {
            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Copy the other parameters off of the given genome
            _genomeFactory = copyFrom._genomeFactory;
            WallStartLocation = copyFrom.WallStartLocation;
            PassageStartLocation = copyFrom.PassageStartLocation;
            EvaluationInfo = new EvaluationInfo(copyFrom.EvaluationInfo.FitnessHistoryLength);
        }

        #endregion

        #region Interface Properties

        public uint Id { get; }

        public int SpecieIdx { get; set; }

        public uint BirthGeneration { get; }

        public EvaluationInfo EvaluationInfo { get; }

        public double Complexity
        {
            get { throw new NotImplementedException(); }
        }

        public CoordinateVector Position
        {
            get { throw new NotImplementedException(); }
        }

        public object CachedPhenome { get; set; }

        #endregion

        #region Maze Properties

        public double WallStartLocation { get; }

        public double PassageStartLocation { get; }

        #endregion

        #region Instance Variables

        private MazeGenomeFactory _genomeFactory;

        #endregion

        #region Public Interface Methods

        public MazeGenome CreateOffspring(uint birthGeneration)
        {
            throw new NotImplementedException();
        }

        public MazeGenome CreateOffspring(MazeGenome parent, uint birthGeneration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Maze Genome Methods

        #endregion
    }
}