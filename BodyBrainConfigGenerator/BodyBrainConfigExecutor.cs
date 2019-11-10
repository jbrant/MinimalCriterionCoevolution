using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace BodyBrainConfigGenerator
{
    internal static class BodyBrainConfigExecutor
    {
        /// <summary>
        ///     Encapsulates configuration parameters specified at runtime.
        /// </summary>
        private static readonly Dictionary<ExecutionParameter, string> _executionConfiguration =
            new Dictionary<ExecutionParameter, string>();

        /// <summary>
        ///     Console logger for reporting execution status.
        /// </summary>
        private static ILog _executionLogger;

        private static void Main(string[] args)
        {
            // Initialise log4net (log to console and file).
            XmlConfigurator.Configure(LogManager.GetRepository(Assembly.GetEntryAssembly()),
                new FileInfo("log4net.config"));

            // Instantiate the execution logger
            _executionLogger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            // Extract the execution parameters and check for any errors (exit application if any found)
            if (ParseAndValidateConfiguration(args) == false)
                Environment.Exit(0);

            _executionLogger.Info("Invocation parameters validated - continuing with configuration file generation.");

            // Extract the parameters
            var experimentName = _executionConfiguration[ExecutionParameter.ExperimentName];
            var run = int.Parse(_executionConfiguration[ExecutionParameter.Run]);
            var configTemplateFile = _executionConfiguration[ExecutionParameter.ConfigTemplateFilePath];
            var outputDirectory = _executionConfiguration[ExecutionParameter.ConfigOutputDirectory];

            // Lookup the current experiment configuration
            var curExperimentConfiguration = ConfigGenerator.LookupExperimentConfiguration(experimentName);

            // Ensure that experiment configuration was found
            if (curExperimentConfiguration == null)
            {
                _executionLogger.Error(
                    $"Unable to lookup experiment configuration for experiment with name [{experimentName}]");
                Environment.Exit(0);
            }

            _executionLogger.Info(
                $"Preparing to execute configuration file generation for experiment [{curExperimentConfiguration.ExperimentName}] run [{run}]");

            // Generate the configuration files
            ConfigGenerator.GenerateSimulationConfigs(curExperimentConfiguration, run, configTemplateFile,
                outputDirectory);

            _executionLogger.Info(
                $"Simulation configuration file generation for experiment [{curExperimentConfiguration.ExperimentName}] and run [{run}] complete");
        }

        /// <summary>
        ///     Populates the execution configuration and checks for any errors in said configuration.
        /// </summary>
        /// <param name="executionArguments">The arguments with which the configuration file executor is being invoked.</param>
        /// <returns>Boolean status indicating whether parsing the configuration suceeded.</returns>
        private static bool ParseAndValidateConfiguration(string[] executionArguments)
        {
            var isConfigurationValid = executionArguments != null;

            // Only continue if there are execution arguments
            if (executionArguments != null && executionArguments.Length > 0)
            {
                foreach (var executionArgument in executionArguments)
                {
                    // Get the key/value pair
                    var parameterValuePair = executionArgument.Split('=');

                    // Attempt to parse the current parameter
                    isConfigurationValid =
                        Enum.TryParse(parameterValuePair[0], true, out ExecutionParameter curParameter);

                    // If the current parameter is not valid, break out of the loop and return
                    if (isConfigurationValid == false)
                    {
                        _executionLogger.Error($"[{parameterValuePair[0]}] is not a valid configuration parameter.");
                        break;
                    }

                    // If the parameter is valid but it already exists in the map, break out of the loop and return
                    if (_executionConfiguration.ContainsKey(curParameter))
                    {
                        _executionLogger.Error(
                            $"Ambiguous configuration - parameter [{curParameter}] has been specified more than once.");
                        break;
                    }

                    switch (curParameter)
                    {
                        // Ensure valid run number was specified
                        case ExecutionParameter.Run:
                            if (int.TryParse(parameterValuePair[1], out _) == false)
                            {
                                _executionLogger.Error($"The value for parameter [{curParameter}] must be an integer.");
                                isConfigurationValid = false;
                            }

                            break;
                    }
                    
                    // If all else checks out, add the parameter to the map
                    _executionConfiguration.Add(curParameter, parameterValuePair[1]);
                }
            }
            // If there are no execution arguments, the configuration is invalid
            else
            {
                isConfigurationValid = false;
            }

            // If the per-parameter configuration is valid but not a full list of parameters were specified, makes sure the necessary ones are present
            if (isConfigurationValid && _executionConfiguration.Count ==
                Enum.GetNames(typeof(ExecutionParameter)).Length == false)
            {
                // Check for existence of experiment names to execute
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ExperimentName) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.ExperimentName}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of run number to execute
                if (_executionConfiguration.ContainsKey(ExecutionParameter.Run) == false)
                {
                    _executionLogger.Error($"Parameter [{ExecutionParameter.Run}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of configuration template parameter
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ConfigTemplateFilePath) == false)
                {
                    _executionLogger.Error(
                        $"Parameter [{ExecutionParameter.ConfigTemplateFilePath}] must be specified.");
                    isConfigurationValid = false;
                }

                // Check for existence of configuration file output directory
                if (_executionConfiguration.ContainsKey(ExecutionParameter.ConfigOutputDirectory) == false)
                {
                    _executionLogger.Error(
                        $"Parameter [{ExecutionParameter.ConfigOutputDirectory}] must be specified.");
                    isConfigurationValid = false;
                }
            }

            // If there's still no problem with the configuration, go ahead and return valid
            if (isConfigurationValid) return true;

            // Log the boiler plate instructions when an invalid configuration is encountered
            _executionLogger.Error(
                "The body/brain configuration file executor invocation must take the following form:");
            _executionLogger.Error(
                $"BodyBrainConfigGenerator.exe {ExecutionParameter.ExperimentName}=experiment {ExecutionParameter.Run}=run {ExecutionParameter.ConfigTemplateFilePath}=file {ExecutionParameter.ConfigOutputDirectory}=directory");

            return false;
        }
    }
}