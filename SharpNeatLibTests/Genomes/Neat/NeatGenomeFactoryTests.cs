using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Genomes.Neat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Network;

namespace SharpNeat.Genomes.Neat.Tests
{
    [TestClass()]
    public class NeatGenomeFactoryTests
    {
        [TestMethod()]
        public void ConectionBufferTest()
        {
            NeatGenomeFactory genomeFactory = new NeatGenomeFactory(3, 1);

            for (uint i = 0; i < 200000; i++)
            {
                ConnectionEndpointsStruct endpointsStruct = new ConnectionEndpointsStruct(i, i+1);
                AddedNeuronGeneStruct addedNeuronGeneStruct = new AddedNeuronGeneStruct(genomeFactory.InnovationIdGenerator);

                genomeFactory.AddedConnectionBuffer.Enqueue(endpointsStruct, i);
                genomeFactory.AddedNeuronBuffer.Enqueue(i, addedNeuronGeneStruct);
            }

            //Assert.Fail();
        }
    }
}