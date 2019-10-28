#region

using System;
using System.Linq;
using MCC_Executor.BodyBrain;
using MCC_Executor.MazeNavigation;

#endregion

namespace MCC_Executor
{
    /// <summary>
    ///     Wrapper class for kicking off specified MCC experiemnts
    /// </summary>
    public class ExperimentExecutor
    {
        private static void Main(string[] args)
        {
            if (args == null || args.Length < 1)
            {
                Console.WriteLine(@"Experiment name must be specified");
                return;
            }

            // Execute the appropriate experiment based on the first parameter
            switch (args[0])
            {
                // Execute maze navigation experiment
                case "maze_navigation":
                    Console.WriteLine(@"Executing the maze navigation experiment");
                    MazeNavigationExperimentExecutor.Execute(args.Skip(1).ToArray());
                    break;
                case "body_brain":
                    Console.WriteLine(@"Executing the robot body-brain coevolution experiment");
                    BodyBrainExperimentExecutor.Execute(args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine(@"Invalid experiment [{0}] specified", args[0]);
                    break;
            }
        }
    }
}