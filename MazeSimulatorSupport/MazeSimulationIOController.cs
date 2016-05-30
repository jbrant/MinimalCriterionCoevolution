#region

using System.IO;
using System.Xml;
using SharpNeat.Decoders;
using SharpNeat.Decoders.Maze;
using SharpNeat.Decoders.Neat;
using SharpNeat.Domains;
using SharpNeat.Genomes.Maze;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeSimulatorSupport
{
    internal class MazeSimulationIOController
    {
        private MazeDecoder _mazeGenomeDecoder;
        private NeatGenomeDecoder _navigatorGenomeDecoder;

        public SimulatorExperimentConfiguration ReadExperimentConfigurationFile(string configurationFile)
        {
            XmlDocument xmlExperimentConfig = new XmlDocument();

            // Parse experiment name out of configuration file
            string experimentName = Path.GetFileNameWithoutExtension(configurationFile);

            // Load experiment configuration file
            xmlExperimentConfig.Load(configurationFile);

            // Get the root element
            XmlElement rootElement = xmlExperimentConfig.DocumentElement;

            // Determine navigator ANN controller activation scheme
            NetworkActivationScheme navigatorActivationScheme = ExperimentUtils.CreateActivationScheme(rootElement,
                "Activation");

            // Create a new navigator genome decoder based on the activation scheme
            _navigatorGenomeDecoder = new NeatGenomeDecoder(navigatorActivationScheme);

            // Read in maze properties
            int mazeHeight = XmlUtils.GetValueAsInt(rootElement, "MazeHeight");
            int mazeWidth = XmlUtils.GetValueAsInt(rootElement, "MazeWidth");
            int mazeScaleMultiplier = XmlUtils.GetValueAsInt(rootElement, "MazeScaleMultiplier");

            // Create a new maze genome decoder based on the maze size
            _mazeGenomeDecoder = new MazeDecoder(mazeHeight, mazeWidth, mazeScaleMultiplier);

            // Read experiment parameteres and return experiment configuration
            return new SimulatorExperimentConfiguration(experimentName, mazeHeight, mazeWidth, mazeScaleMultiplier,
                navigatorActivationScheme, XmlUtils.GetValueAsInt(rootElement, "MaxTimesteps"),
                XmlUtils.GetValueAsInt(rootElement, "MinSuccessDistance"));
        }

        public IBlackBox ReadNavigatorGenomeFile(string navigatorGenomeFile)
        {
            NeatGenome navigatorGenome;

            // Parse the NEAT genome file
            using (XmlReader xmlReader = XmlReader.Create(navigatorGenomeFile))
            {
                // NOTE: the genome factory input/output counts are arbitary, but likely accurate
                // Regardless, they don't make a difference because offspring are not produced during the simulation
                navigatorGenome = NeatGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, false,
                    new NeatGenomeFactory(10, 2));
            }

            // Decode navigator genome to its network phenotype
            IBlackBox navigatorAnn = _navigatorGenomeDecoder.Decode(navigatorGenome);

            return navigatorAnn;
        }

        public MazeStructure ReadMazeGenomeFile(string mazeGenomeFile)
        {
            // TODO: Needs to return MazeStructure

            MazeGenome mazeGenome;

            // Parse the maze genome file
            using (XmlReader xmlReader = XmlReader.Create(mazeGenomeFile))
            {
                mazeGenome = MazeGenomeXmlIO.ReadSingleGenomeFromRoot(xmlReader, new MazeGenomeFactory());
            }

            // Decode maze genome to a maze structure (phenotype)
            MazeStructure mazeStructure = _mazeGenomeDecoder.Decode(mazeGenome);

            return mazeStructure;
        }
    }
}