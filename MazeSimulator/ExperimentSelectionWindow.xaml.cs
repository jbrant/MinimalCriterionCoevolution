using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MazeSimulatorSupport;

namespace MazeSimulator
{
    /// <summary>
    /// Interaction logic for ExperimentSelectionWindow.xaml
    /// </summary>
    public partial class ExperimentSelectionWindow : Window
    {
        private readonly SimulationInstanceDirector _simulationInstanceDirector;

        public ExperimentSelectionWindow(SimulationInstanceDirector simulationInstanceDirector)
        {            
            InitializeComponent();

            // Set the reference to the experiment directory
            _simulationInstanceDirector = simulationInstanceDirector;

            // Pull the list of experiment names to populate the configuration combo box
            _simulationInstanceDirector.RetrieveExperimentConfigurationNames(
                Properties.Settings.Default.ExperimentDbConnectionString);
        }
    }
}
