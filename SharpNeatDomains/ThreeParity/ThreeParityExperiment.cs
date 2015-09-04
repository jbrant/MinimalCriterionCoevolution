using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SharpNeat.Core;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.Genomes.Neat;
using SharpNeat.Phenomes;

namespace SharpNeat.Domains.ThreeParity
{
    class ThreeParityExperiment : INeatExperiment
    {
        public string Name { get; private set; }

        public string Description { get; private set; }

        public int InputCount => 8;

        public int OutputCount => 1;        

        public int DefaultPopulationSize { get; private set; }        

        public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters
        {
            get { throw new NotImplementedException(); }
        }

        public NeatGenomeParameters NeatGenomeParameters
        {
            get { throw new NotImplementedException(); }
        }

        public void Initialize(string name, XmlElement xmlConfig)
        {
            throw new NotImplementedException();
        }

        public List<NeatGenome> LoadPopulation(XmlReader xr)
        {
            throw new NotImplementedException();
        }

        public void SavePopulation(XmlWriter xw, IList<NeatGenome> genomeList)
        {
            throw new NotImplementedException();
        }

        public IGenomeDecoder<NeatGenome, IBlackBox> CreateGenomeDecoder()
        {
            throw new NotImplementedException();
        }

        public IGenomeFactory<NeatGenome> CreateGenomeFactory()
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
        {
            throw new NotImplementedException();
        }

        public INeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
        {
            throw new NotImplementedException();
        }
    }
}
