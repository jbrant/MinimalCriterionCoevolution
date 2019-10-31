#region

using System.Collections.Generic;

#endregion

namespace SharpNeat.Loggers
{
    public static class EvolutionFieldElements
    {
        /// <summary>
        ///     The generation of the observation.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The run phase (initialization or primary) at the time of the observation.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(1, "Run Phase");

        /// <summary>
        ///     The number of species in the population.
        /// </summary>
        public static readonly FieldElement SpecieCount = new FieldElement(2, "Specie Count");

        /// <summary>
        ///     The number of offspring that were produced asexually.
        /// </summary>
        public static readonly FieldElement AsexualOffspringCount = new FieldElement(3, "Asexual Offspring Count");

        /// <summary>
        ///     The number of offspring that were produced sexually.
        /// </summary>
        public static readonly FieldElement SexualOffspringCount = new FieldElement(4, "Sexual Offspring Count");

        /// <summary>
        ///     The number of offspring that were produced via interspecies mating.
        /// </summary>
        public static readonly FieldElement InterspeciesOffspringCount = new FieldElement(5,
            "Interspecies Offspring Count");

        /// <summary>
        ///     The total number of offspring that were produced.
        /// </summary>
        public static readonly FieldElement TotalOffspringCount = new FieldElement(6, "Total Offspring Count");

        /// <summary>
        ///     The current population size.
        /// </summary>
        public static readonly FieldElement PopulationSize = new FieldElement(7, "Population Size");

        /// <summary>
        ///     The current minimal criteria threshold (only changes if dynamic).
        /// </summary>
        public static readonly FieldElement
            MinimalCriteriaThreshold = new FieldElement(8, "Minimal Criteria Threshold");

        /// <summary>
        ///     The X position of the minimal criteria point in euclidean space.
        /// </summary>
        public static readonly FieldElement MinimalCriteriaPointX = new FieldElement(9,
            "Minimal Criteria MazeStructurePoint X");

        /// <summary>
        ///     The Y position of the minimal criteria point in euclidean space.
        /// </summary>
        public static readonly FieldElement MinimalCriteriaPointY = new FieldElement(10,
            "Minimal Criteria MazeStructurePoint Y");

        /// <summary>
        ///     The maximum fitness in the current population.
        /// </summary>
        public static readonly FieldElement MaxFitness = new FieldElement(11, "Max Fitness");

        /// <summary>
        ///     The mean fitness of the current population.
        /// </summary>
        public static readonly FieldElement MeanFitness = new FieldElement(12, "Mean Fitness");

        /// <summary>
        ///     The mean fitness of the best-performing genome from each species.
        /// </summary>
        public static readonly FieldElement MeanSpecieChampFitness = new FieldElement(13, "Mean Specie Champ Fitness");

        /// <summary>
        ///     The least complex organism in the current population.
        /// </summary>
        public static readonly FieldElement MinComplexity = new FieldElement(14, "Min Complexity");

        /// <summary>
        ///     The most complex organism in the current populuation.
        /// </summary>
        public static readonly FieldElement MaxComplexity = new FieldElement(15, "Max Complexity");

        /// <summary>
        ///     The mean complexity of all organisms in the current population.
        /// </summary>
        public static readonly FieldElement MeanComplexity = new FieldElement(16, "Mean Complexity");

        /// <summary>
        ///     The size of the smallest extant species.
        /// </summary>
        public static readonly FieldElement MinSpecieSize = new FieldElement(17, "Min Specie Size");

        /// <summary>
        ///     The size of the largest extant species.
        /// </summary>
        public static readonly FieldElement MaxSpecieSize = new FieldElement(18, "Max Specie Size");

        /// <summary>
        ///     The number of evaluations that were executed at the time of the observation.
        /// </summary>
        public static readonly FieldElement TotalEvaluations = new FieldElement(19, "Total Evaluations");

        /// <summary>
        ///     The average number of evaluations executed per second during the current generation/batch.
        /// </summary>
        public static readonly FieldElement EvaluationsPerSecond = new FieldElement(20, "Evaluations per Second");

        /// <summary>
        ///     The ID of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeGenomeId = new FieldElement(21, "Champ Genome ID");

        /// <summary>
        ///     The fitness of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeFitness = new FieldElement(22, "Champ Genome Fitness");

        /// <summary>
        ///     The birth generation of the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeBirthGeneration = new FieldElement(23,
            "Champ Genome Birth Generation");

        /// <summary>
        ///     The number of connections in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeConnectionGeneCount = new FieldElement(24,
            "Champ Genome Connection Gene Count");

        /// <summary>
        ///     The number of neurons in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeNeuronGeneCount = new FieldElement(25,
            "Champ Genome Neuron Gene Count");

        /// <summary>
        ///     The total number of genes in the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeTotalGeneCount = new FieldElement(26,
            "Champ Genome Total Gene Count");

        /// <summary>
        ///     The total number of evaluations undergone by the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeEvaluationCount = new FieldElement(27,
            "Champ Genome Evaluation Count");

        /// <summary>
        ///     The genome XML definition for the global best performing genome.
        /// </summary>
        public static readonly FieldElement ChampGenomeXml = new FieldElement(28, "Champ Genome XML");

        /// <summary>
        ///     Pre-constructs an evolution log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evolution log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvolutionFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {RunPhase, true},
                {SpecieCount, true},
                {AsexualOffspringCount, true},
                {SexualOffspringCount, true},
                {InterspeciesOffspringCount, true},
                {TotalOffspringCount, true},
                {PopulationSize, true},
                {MinimalCriteriaThreshold, true},
                {MinimalCriteriaPointX, true},
                {MinimalCriteriaPointY, true},
                {MaxFitness, true},
                {MeanFitness, true},
                {MeanSpecieChampFitness, true},
                {MinComplexity, true},
                {MaxComplexity, true},
                {MeanComplexity, true},
                {MinSpecieSize, true},
                {MaxSpecieSize, true},
                {TotalEvaluations, true},
                {EvaluationsPerSecond, true},
                {ChampGenomeGenomeId, true},
                {ChampGenomeFitness, true},
                {ChampGenomeBirthGeneration, true},
                {ChampGenomeConnectionGeneCount, true},
                {ChampGenomeNeuronGeneCount, true},
                {ChampGenomeTotalGeneCount, true},
                {ChampGenomeEvaluationCount, true},
                {ChampGenomeXml, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing data related to population statistics per generation/batch for maze navigation experiments.
    /// </summary>
    public static class MazeNavEvolutionFieldElements
    {
        /// <summary>
        ///     The minimum number of walls in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinWalls = new FieldElement(29, "Min Walls");

        /// <summary>
        ///     The maximum number of walls in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxWalls = new FieldElement(30, "Max Walls");

        /// <summary>
        ///     The mean number of walls among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanWalls = new FieldElement(31, "Mean Walls");

        /// <summary>
        ///     The minimum number of waypoints in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinWaypoints = new FieldElement(32, "Min Waypoints");

        /// <summary>
        ///     The maximum number of waypoints in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxWaypoints = new FieldElement(33, "Max Waypoints");

        /// <summary>
        ///     The mean number of waypoints among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanWaypoints = new FieldElement(34, "Mean Waypoints");

        /// <summary>
        ///     The minimum number of junctures in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinJunctures = new FieldElement(35, "Min Junctures");

        /// <summary>
        ///     The maximum number of junctures in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxJunctures = new FieldElement(36, "Max Junctures");

        /// <summary>
        ///     The mean number of junctures among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanJunctures = new FieldElement(37, "Mean Junctures");

        /// <summary>
        ///     The minimum number of openings facing the trajectory in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinTrajectoryFacingOpenings =
            new FieldElement(38, "Min Trajectory Facing Openings");

        /// <summary>
        ///     The maximum number of openings facing the trajectory in a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxTrajectoryFacingOpenings =
            new FieldElement(39, "Max Trajectory Facing Openings");

        /// <summary>
        ///     The mean number of openings facing the trajectory among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanTrajectoryFacingOpenings =
            new FieldElement(40, "Mean Trajectory Facing Openings");

        /// <summary>
        ///     The minimum height of a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinHeight = new FieldElement(41, "Min Height");

        /// <summary>
        ///     The maximum height of a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxHeight = new FieldElement(42, "Max Height");

        /// <summary>
        ///     The mean height among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanHeight = new FieldElement(43, "Mean Height");

        /// <summary>
        ///     The minimum width of a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MinWidth = new FieldElement(44, "Min Width");

        /// <summary>
        ///     The maximum width of a maze within the maze population.
        /// </summary>
        public static readonly FieldElement MaxWidth = new FieldElement(45, "Max Width");

        /// <summary>
        ///     The mean width among mazes within the maze population.
        /// </summary>
        public static readonly FieldElement MeanWidth = new FieldElement(46, "Mean Width");

        /// <summary>
        ///     Pre-constructs an evolution log field enable map with all of the fields enabled by default (for maze navigation
        ///     experiments).
        /// </summary>
        /// <returns>Evolution log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvolutionFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {MinWalls, true},
                {MaxWalls, true},
                {MeanWalls, true},
                {MinWaypoints, true},
                {MaxWaypoints, true},
                {MeanWaypoints, true},
                {MinJunctures, true},
                {MaxJunctures, true},
                {MeanJunctures, true},
                {MinTrajectoryFacingOpenings, true},
                {MaxTrajectoryFacingOpenings, true},
                {MeanTrajectoryFacingOpenings, true},
                {MinHeight, true},
                {MaxHeight, true},
                {MeanHeight, true},
                {MinWidth, true},
                {MaxWidth, true},
                {MeanWidth, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing data related to population statistics per generation/batch for body/brain experiments.
    /// </summary>
    public static class BodyBrainEvolutionFieldElements
    {
        /// <summary>
        ///     The minimum number of voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MinVoxels = new FieldElement(29, "Min Voxels");

        /// <summary>
        ///     The maximum number of voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MaxVoxels = new FieldElement(30, "Max Voxels");

        /// <summary>
        ///     The mean number of voxels within the body population.
        /// </summary>
        public static readonly FieldElement MeanVoxels = new FieldElement(31, "Mean Voxels");

        /// <summary>
        ///     The minimum voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MinFullProportion = new FieldElement(32, "Min Full Proportion");

        /// <summary>
        ///     The maximum voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MaxFullProportion = new FieldElement(33, "Max Full Proportion");

        /// <summary>
        ///     The mean voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MeanFullProportion = new FieldElement(34, "Mean Full Proportion");

        /// <summary>
        ///     The minimum number of active voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MinActiveVoxels = new FieldElement(35, "Min Active Voxels");

        /// <summary>
        ///     The maximum number of active voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MaxActiveVoxels = new FieldElement(36, "Max Active Voxels");

        /// <summary>
        ///     The mean number of active voxels within the body population.
        /// </summary>
        public static readonly FieldElement MeanActiveVoxels = new FieldElement(37, "Mean Active Voxels");

        /// <summary>
        ///     The minimum number of passive voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MinPassiveVoxels = new FieldElement(38, "Min Passive Voxels");

        /// <summary>
        ///     The maximum number of passive voxels in a given voxel body within the body population.
        /// </summary>
        public static readonly FieldElement MaxPassiveVoxels = new FieldElement(39, "Max Passive Voxels");

        /// <summary>
        ///     The mean number of passive voxels within the body population.
        /// </summary>
        public static readonly FieldElement MeanPassiveVoxels = new FieldElement(40, "Mean Passive Voxels");

        /// <summary>
        ///     The minimum active voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MinActiveVoxelProportion =
            new FieldElement(41, "Min Active Voxel Proportion");

        /// <summary>
        ///     The maximum active voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MaxActiveVoxelProportion =
            new FieldElement(42, "Max Active Voxel Proportion");

        /// <summary>
        ///     The mean active voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MeanActiveVoxelProportion =
            new FieldElement(43, "Mean Active Voxel Proportion");

        /// <summary>
        ///     The minimum passive voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MinPassiveVoxelProportion =
            new FieldElement(44, "Min Passive Voxel Proportion");

        /// <summary>
        ///     The maximum passive voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MaxPassiveVoxelProportion =
            new FieldElement(45, "Max Passive Voxel Proportion");

        /// <summary>
        ///     The mean passive voxel proportion within the body population.
        /// </summary>
        public static readonly FieldElement MeanPassiveVoxelProportion =
            new FieldElement(46, "Mean Passive Voxel Proportion");

        /// <summary>
        ///     Pre-constructs an evolution log field enable map with all of the fields enabled by default (for body/brain
        ///     experiments).
        /// </summary>
        /// <returns>Evolution log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvolutionFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {MinVoxels, true},
                {MaxVoxels, true},
                {MeanVoxels, true},
                {MinFullProportion, true},
                {MaxFullProportion, true},
                {MeanFullProportion, true},
                {MinActiveVoxels, true},
                {MaxActiveVoxels, true},
                {MeanActiveVoxels, true},
                {MinPassiveVoxels, true},
                {MaxPassiveVoxels, true},
                {MeanPassiveVoxels, true},
                {MinActiveVoxelProportion, true},
                {MaxActiveVoxelProportion, true},
                {MeanActiveVoxelProportion, true},
                {MinPassiveVoxelProportion, true},
                {MaxPassiveVoxelProportion, true},
                {MeanPassiveVoxelProportion, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing data related to individual organism evaluations.
    /// </summary>
    public static class EvaluationFieldElements
    {
        /// <summary>
        ///     The generation of the observation.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The number of evaluations that were executed at the time of the observation.
        /// </summary>
        public static readonly FieldElement EvaluationCount = new FieldElement(1, "Evaluation Count");

        /// <summary>
        ///     The run phase (initialization or primary) at the time of the observation.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(2, "Run Phase");

        /// <summary>
        ///     Whether or not the organism was considered viable (i.e. satisfied some objective/non-objective criterion).
        /// </summary>
        public static readonly FieldElement IsViable = new FieldElement(3, "Is Viable");

        /// <summary>
        ///     Whether or not the experiment stop condition was satisfied.
        /// </summary>
        public static readonly FieldElement StopConditionSatisfied = new FieldElement(4, "Stop Condition Satisfied");

        /// <summary>
        ///     The distance to the objective location.
        /// </summary>
        public static readonly FieldElement DistanceToTarget = new FieldElement(5, "Distance to Target");

        /// <summary>
        ///     The X position of the organism in euclidean space.
        /// </summary>
        public static readonly FieldElement AgentXLocation = new FieldElement(6, "Agent X Location");

        /// <summary>
        ///     The Y position of the organism in euclidean space.
        /// </summary>
        public static readonly FieldElement AgentYLocation = new FieldElement(7, "Agent Y Location");

        /// <summary>
        ///     The simulation time consumed by executing the trial.
        /// </summary>
        public static readonly FieldElement SimTime = new FieldElement(8, "Simulation Time");

        /// <summary>
        ///     Pre-constructs an evaluation log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evaluation log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateEvaluationFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {EvaluationCount, true},
                {RunPhase, true},
                {IsViable, true},
                {StopConditionSatisfied, true},
                {DistanceToTarget, true},
                {AgentXLocation, true},
                {AgentYLocation, true},
                {SimTime, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing the extant genomes (denoted by their ID) during a point in the run, given by the run phase and
    ///     generation..
    /// </summary>
    public static class PopulationFieldElements
    {
        /// <summary>
        ///     The run phase (i.e. initialization or primary) during which the given observation executed.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(0, "Run Phase");

        /// <summary>
        ///     The generation in which the given population is extant.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(1, "Generation");

        /// <summary>
        ///     The ID of the genome definition being logged.
        /// </summary>
        public static readonly FieldElement GenomeId = new FieldElement(2, "Genome ID");

        /// <summary>
        ///     The unique identifier for the species of which the genome is a member.
        /// </summary>
        public static readonly FieldElement SpecieId = new FieldElement(3, "Specie ID");

        /// <summary>
        ///     Pre-constructs a population log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Population log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulatePopulationFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {RunPhase, true},
                {Generation, true},
                {GenomeId, true},
                {SpecieId, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing the outcome and details of a simulated evaluation trial.
    /// </summary>
    public static class SimulationTrialFieldElements
    {
        /// <summary>
        ///     The generation in which the given genome is extant.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The ID of the genome definition being logged.
        /// </summary>
        public static readonly FieldElement GenomeId = new FieldElement(1, "Genome ID");

        /// <summary>
        ///     The ID of the genome with which the current genome was paired for evaluation.
        /// </summary>
        public static readonly FieldElement PairedGenomeId = new FieldElement(2, "Paired Genome ID");

        /// <summary>
        ///     Boolean indicator of whether the given trial was successful.
        /// </summary>
        public static readonly FieldElement IsSuccessful = new FieldElement(3, "Is Successful");

        /// <summary>
        ///     The distance either between the ending point of the simulation and the target location, or the total distance traveled.
        /// </summary>
        public static readonly FieldElement Distance = new FieldElement(4, "Distance");

        /// <summary>
        ///     The number of simulated timesteps in the trial.
        /// </summary>
        public static readonly FieldElement NumTimesteps = new FieldElement(5, "Num Timesteps");

        /// <summary>
        ///     Pre-constructs a simulation trial log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Simulation trial log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateSimulationTrialFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {GenomeId, true},
                {PairedGenomeId, true},
                {IsSuccessful, true},
                {Distance, true},
                {NumTimesteps, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing the XML definition of genomes throughout the course of a run.
    /// </summary>
    public static class GenomeFieldElements
    {
        /// <summary>
        ///     The run phase (i.e. initialization or primary) during which the given observation executed.
        /// </summary>
        public static readonly FieldElement RunPhase = new FieldElement(0, "Run Phase");

        /// <summary>
        ///     The ID of the genome definition being logged.
        /// </summary>
        public static readonly FieldElement GenomeId = new FieldElement(1, "Genome ID");

        /// <summary>
        ///     The XML definition of the genome.
        /// </summary>
        public static readonly FieldElement GenomeXml = new FieldElement(2, "Genome XML");

        /// <summary>
        ///     Pre-constructs an evaluation log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Evaluation log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateGenomeFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {RunPhase, true},
                {GenomeId, true},
                {GenomeXml, true}
            };
        }
    }

    /// <summary>
    ///     Fields capturing the resource usage of mazes throughout the course of a run.
    /// </summary>
    public static class ResourceUsageFieldElements
    {
        /// <summary>
        ///     The generation at which the resource usage was recorded.
        /// </summary>
        public static readonly FieldElement Generation = new FieldElement(0, "Generation");

        /// <summary>
        ///     The ID of the genome whose usage is being logged.
        /// </summary>
        public static readonly FieldElement GenomeId = new FieldElement(1, "Genome ID");

        /// <summary>
        ///     The resource usage count of the given genome.
        /// </summary>
        public static readonly FieldElement UsageCount = new FieldElement(2, "Usage Count");

        /// <summary>
        ///     Pre-constructs a resource usage log field enable map with all of the fields enabled by default.
        /// </summary>
        /// <returns>Resource usage log field enable map with all fields enabled.</returns>
        public static Dictionary<FieldElement, bool> PopulateResourceUsageFieldElementsEnableMap()
        {
            return new Dictionary<FieldElement, bool>
            {
                {Generation, true},
                {GenomeId, true},
                {UsageCount, true}
            };
        }
    }

    /// <summary>
    ///     Encapsulates the position and name of a field within an experiment data log.
    /// </summary>
    public class FieldElement
    {
        /// <summary>
        ///     FieldElement constructor.
        /// </summary>
        /// <param name="position">The absolute position of the field within the log file/table.</param>
        /// <param name="friendlyName">The name of the field.</param>
        public FieldElement(int position, string friendlyName)
        {
            Position = position;
            FriendlyName = friendlyName;
        }

        /// <summary>
        ///     The absolute position of the field within the log file/table.
        /// </summary>
        public int Position { get; }

        /// <summary>
        ///     The name of the field.
        /// </summary>
        public string FriendlyName { get; }
    }
}