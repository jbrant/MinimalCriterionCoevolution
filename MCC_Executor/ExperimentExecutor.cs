#region

using System;
using System.Linq;
using SharpNeatConsole;

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
            // Execute the appropriate experiment based on the first parameter
            switch (args[0])
            {
                // Execute maze navigation experiment
                case "maze_navigation":
                    Console.WriteLine("Executing the maze navigation experiment");
                    MazeNavigationExperimentExecutor.execute(args.Skip(1).ToArray());
                    break;
                default:
                    Console.WriteLine("Invalid experiment specified");
                    break;
            }
        }
    }
}