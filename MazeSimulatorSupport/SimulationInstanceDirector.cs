#region

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpNeat.Phenomes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeSimulatorSupport
{
    public class SimulationInstanceDirector
    {
        #region Controllers and utility class references

        private MazeSimulationGraphicsController _mazeSimulationGraphicsController;
        private MazeSimulationIOController _mazeSimulationIoController;

        #endregion

        #region Configuration/instance variables

        private SimulatorExperimentConfiguration _simulatorExperimentConfiguration;
        private IBlackBox _navigatorAnnController;
        private MazeStructure _mazeStructure;

        #endregion

        #region Public control methods

        public void SetupNewExecution(int canvasHeight, int canvasWidth, SolidColorBrush wallBrush, int wallThickness)
        {
            _mazeSimulationIoController = new MazeSimulationIOController();
            _mazeSimulationGraphicsController = new MazeSimulationGraphicsController(canvasHeight, canvasWidth,
                wallBrush, wallThickness);
        }

        public void LoadExperimentConfiguration(string experimentConfigurationFile)
        {
            // Note that this will throw an exception up to the caller if configuration file read fails
            _simulatorExperimentConfiguration =
                _mazeSimulationIoController.ReadExperimentConfigurationFile(experimentConfigurationFile);
        }

        public void LoadMazeNavigator(string navigatorGenomeFile)
        {
            _navigatorAnnController =
                _mazeSimulationIoController.ReadNavigatorGenomeFile(navigatorGenomeFile);
        }

        public void LoadMaze(string mazeGenomeFile)
        {
            _mazeStructure = _mazeSimulationIoController.ReadMazeGenomeFile(mazeGenomeFile);
        }

        public List<Line> ConstructMazeWalls()
        {
            return _mazeSimulationGraphicsController.GetMazeWalls(_mazeStructure.Walls,
                _simulatorExperimentConfiguration.MazeHeight, _simulatorExperimentConfiguration.MazeWidth);
        }

        public bool IsConnectionStringValid(string connectionString)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    // Attempt to open connection (exception is raised if this fails)
                    sqlConnection.Open();
                }

                return true;
            }
            catch (SqlException)
            {
                // If exception was thrown it means there was a problem connecting or the connection string is invalid
                return false;
            }
        }

        public string GetEntityFormatConnectionString(string connectionString)
        {
            return _mazeSimulationIoController.BuildEntityConnectionString(connectionString);
        }

        public List<string> RetrieveExperimentConfigurationNames(string connectionString)
        {
            return _mazeSimulationIoController.GetDatabaseExperimentNames(connectionString);
        }

        #endregion
    }
}