using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperimentEntities;

namespace TestDbConnectFromLinux
{
    class Program
    {
        static void Main(string[] args)
        {
            int testExperimentCount;

            using (ExperimentDataEntities context = new ExperimentDataEntities())
            {
                testExperimentCount = context.ExperimentDictionaries.Count();
            }

            Console.Out.WriteLine(string.Format("Experiment Count: {0}", testExperimentCount));
        }
    }
}
