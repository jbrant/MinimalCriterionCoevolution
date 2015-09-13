#region

using System;
using System.Diagnostics;
using SharpNeat.Core;
using SharpNeat.Phenomes;

#endregion

namespace SharpNeat.Domains.ThreeParity
{
    /// <summary>
    ///     Evaluator/fitness function for the three-parity XOR problem.
    /// </summary>
    public class ThreeParityEvaluator : IPhenomeEvaluator<IBlackBox, FitnessInfo>
    {
        const double StopFitness = 100.0;

        /// <summary>
        ///     The number of evaluations conducted over the course of a run.
        /// </summary>
        public ulong EvaluationCount { get; private set; }

        /// <summary>
        ///     Whether or not the problem has been solved within an acceptance margin of error.
        /// </summary>
        public bool StopConditionSatisfied { get; private set; }

        /// <summary>
        ///     Evalutes the given black box optimizer against the three parity XOR domain and returns the fitness score.
        /// </summary>
        /// <param name="phenome">The phenome/black box (ANN) under evaluation.</param>
        /// <returns>The fitness score of the given black box optimizer.</returns>
        public FitnessInfo Evaluate(IBlackBox phenome)
        {
            double fitness = 0;
            bool success = true;

            // Get the reference to the black box input array
            ISignalArray inputValues = phenome.InputSignalArray;
            ISignalArray outputValues = phenome.OutputSignalArray;

            // Increment the number of evaluations
            EvaluationCount++;

            // Evaluate the ANN on every combination
            for (int inputConfiguration = 0; inputConfiguration < 8; inputConfiguration++)
            {
                // Make a copy of the current input configuration so we don't lose it in the computation below
                int curInputConfiguration = inputConfiguration;

                // Bit mask/shift the input configuration to get the binary input values
                for (int inputBit = 0; inputBit < 3; inputBit++)
                {
                    inputValues[inputBit] = curInputConfiguration & 0x1;
                    curInputConfiguration >>= 1;
                }

                // Activate the network
                phenome.Activate();

                // If activation causes the network to be in an invalid state, assign a fitness score of zero
                if (!phenome.IsStateValid)
                {
                    return FitnessInfo.Zero;
                }

                // Read output signal.
                double output = outputValues[0];
                Debug.Assert(output >= 0.0, "Output cannot be negative.");

                // If the actual output should be true, reward based on the network's output distance to the desired 
                // output of 1
                if (((Convert.ToByte(inputValues[0]) ^ Convert.ToByte(inputValues[1])) ^ Convert.ToByte(inputValues[2])) !=
                    0)
                {
                    // Since the desired output is 1, the square error ends up being the distance from the desired
                    // value of 1.  Also, since we want to reward lower error by inversely maximizing the fitness
                    // score, we subtract the squared error from 1
                    fitness += 1.0 - ((1.0 - output) * (1.0 - output));

                    // Test if output is within certain margin of error
                    if (output < 0.5)
                    {
                        success = false;
                    }
                }
                // Otherwise, the actual output should be false, so reward based on the network's output distance 
                // to the desired output of 0
                else
                {
                    // since the desired output is 0, the squared error is calculated as the square of the ANN output
                    // subtracted from 1
                    fitness += 1.0 - (output * output);

                    // Test if output is within certain margin of error
                    if (output >= 0.5)
                    {
                        success = false;
                    }
                }

                // Reset the network state ready for next test case
                phenome.ResetState();
            }

            // If the correct answer was produced in all cases, a solution has been found
            // so training can stop
            if (success)
            {
                StopConditionSatisfied = true;
            }

            return new FitnessInfo(fitness, fitness);
        }

        /// <summary>
        ///     Resets the state of the evaluator (not needed for this experiment).
        /// </summary>
        public void Reset()
        {
        }
    }
}