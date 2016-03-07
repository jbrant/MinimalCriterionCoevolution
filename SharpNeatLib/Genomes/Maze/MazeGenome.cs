#region

using System;
using System.Collections.Generic;
using SharpNeat.Core;
using SharpNeat.Utility;

#endregion

namespace SharpNeat.Genomes.Maze
{
    public class MazeGenome : IGenome<MazeGenome>
    {
        #region Instance Variables

        private readonly MazeGenomeFactory _genomeFactory;

        #endregion

        #region Maze Properties

        public List<MazeGene> GeneList { get; }

        #endregion

        #region Maze Genome Constructors

        public MazeGenome(MazeGenomeFactory genomeFactory, uint id, uint birthGeneration)
        {
            // Set the genome factory
            _genomeFactory = genomeFactory;

            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Create new evaluation info with no fitness history
            EvaluationInfo = new EvaluationInfo(0);

            // Instantiate new gene list
            GeneList = new List<MazeGene>();
        }

        public MazeGenome(MazeGenome copyFrom, uint id, uint birthGeneration)
        {
            // Set the unique genome ID and the birth generation
            Id = id;
            BirthGeneration = birthGeneration;

            // Copy the other parameters off of the given genome
            _genomeFactory = copyFrom._genomeFactory;
            GeneList = copyFrom.GeneList;
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

        #region Public Interface Methods

        public MazeGenome CreateOffspring(uint birthGeneration)
        {
            // Make a new genome that is a copy of this one but with a new genome ID
            MazeGenome offspring = _genomeFactory.CreateGenomeCopy(this, _genomeFactory.GenomeIdGenerator.NextId,
                birthGeneration);

            // Mutate the new genome
            offspring.Mutate();

            return offspring;
        }

        public MazeGenome CreateOffspring(MazeGenome parent, uint birthGeneration)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Maze Genome Methods

        private void Mutate()
        {
            // Get random mutation to perform
            int outcome = RouletteWheel.SingleThrow(_genomeFactory.MazeGenomeParameters.RouletteWheelLayout,
                _genomeFactory.Rng);

            switch (outcome)
            {
                case 0:
                    MutateWallStartLocations();
                    break;
                case 1:
                    MutatePassageStartLocations();
                    break;
                case 2:
                    MutateAddWall();
                    break;
            }
        }

        private void MutateWallStartLocations()
        {
            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            foreach (MazeGene mazeGene in GeneList)
            {
                if (_genomeFactory.Rng.NextDouble() <
                    _genomeFactory.MazeGenomeParameters.MutateWallStartLocationProbability)
                {
                    mazeGene.WallLocation = BoundStartLocation(mazeGene.WallLocation +
                                                                    (((_genomeFactory.Rng.NextDouble()*2) - 1)*
                                                                     _genomeFactory.MazeGenomeParameters
                                                                         .PerturbanceMagnitude));
                }
            }
        }

        private void MutatePassageStartLocations()
        {
            // Don't try to mutate if the gene list is empty
            if (GeneList.Count <= 0)
                return;

            foreach (MazeGene mazeGene in GeneList)
            {
                if (_genomeFactory.Rng.NextDouble() <
                    _genomeFactory.MazeGenomeParameters.MutatePassageStartLocationProbability)
                {
                    mazeGene.PassageLocation = BoundStartLocation(mazeGene.PassageLocation +
                                                                       (((_genomeFactory.Rng.NextDouble()*2) - 1)*
                                                                        _genomeFactory.MazeGenomeParameters
                                                                            .PerturbanceMagnitude));
                }
            }
        }

        private void MutateAddWall()
        {
            // Generate new wall and passage start locations
            double newWallStartLocation = _genomeFactory.Rng.NextDoubleNonZero();
            double newPassageStartLocation = _genomeFactory.Rng.NextDoubleNonZero();

            // Add new gene to the genome
            GeneList.Add(new MazeGene(newWallStartLocation, newPassageStartLocation, _genomeFactory.Rng.NextBool()));
        }

        private double BoundStartLocation(double proposedLocation)
        {
            if (proposedLocation < 0)
                return 0;
            if (proposedLocation > 1)
                return 1;
            return proposedLocation;
        }

        #endregion
    }
}