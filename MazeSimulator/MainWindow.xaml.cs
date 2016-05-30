#region

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MazeSimulatorSupport;
using Microsoft.Win32;

#endregion

namespace MazeSimulator
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SimulationInstanceDirector _simInstanceDirector;
        private readonly Rectangle testRect;

        public MainWindow()
        {
            InitializeComponent();

            // Instantiate the controllers
            _simInstanceDirector = new SimulationInstanceDirector();

            // Configure experiment instance
            _simInstanceDirector.SetupNewExecution((int) SimulationCanvas.Height, (int) SimulationCanvas.Width,
                Brushes.LightSteelBlue, 2);

            // TODO: Technically, this stuff should only be called when maze genome file is referenced
            // Get maze boundaries/walls
            /*foreach (Line mazeWall in _mazeSimulationGraphicsController.GetMazeWalls())
            {
                SimulationCanvas.Children.Add(mazeWall);
            }*/

            testRect = new Rectangle();
            testRect.Width = 20;
            testRect.Height = 20;
            testRect.Fill = Brushes.Lime;
            SimulationCanvas.Children.Add(testRect);

            Canvas.SetLeft(testRect, 100);

            Canvas.GetLeft(testRect);

            // TODO: This is the reference code for setting up the timer for simulation steps
            /*
            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler(dispatchTimer_Tick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer.Start();
            */
        }

        private void dispatchTimer_Tick(object sender, EventArgs e)
        {
            Canvas.SetLeft(testRect, Canvas.GetLeft(testRect) + 1);
        }

        private void CloseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StartMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void StopMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void PauseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void OpenDatabaseMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void LoadNavigatorGenomeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Create open file dialog and filter to XML files
            OpenFileDialog openNavigatorGenomeFileDialog = new OpenFileDialog();
            openNavigatorGenomeFileDialog.Filter = "XML files (*.xml) | *.xml";

            if (openNavigatorGenomeFileDialog.ShowDialog() == true)
            {
                try
                {
                    _simInstanceDirector.LoadMazeNavigator(openNavigatorGenomeFileDialog.FileName);
                }
                catch (Exception navGenomeLoadException)
                {
                    MessageBox.Show(string.Format("An error occurred while loading the navigator genome: {0}",
                        navGenomeLoadException.Message), "Navigator Genome Load Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void LoadMazeGenomeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Create open file dialog and filter to XML files
            OpenFileDialog openMazeGenomeFileDialog = new OpenFileDialog();
            openMazeGenomeFileDialog.Filter = "XML files (*.xml) | *.xml";

            if (openMazeGenomeFileDialog.ShowDialog() == true)
            {
                try
                {
                    _simInstanceDirector.LoadMaze(openMazeGenomeFileDialog.FileName);

                    // TODO: Need to add some code to display the maze here
                    foreach (Line mazeWall in _simInstanceDirector.ConstructMazeWalls())
                    {
                        SimulationCanvas.Children.Add(mazeWall);
                    }
                }
                catch (Exception mazeGenomeLoadException)
                {
                    MessageBox.Show(string.Format("An error occurred while loading the maze genome: {0}",
                        mazeGenomeLoadException.Message), "Maze Genome Load Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private void LoadExperimentConfigurationMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            // Create open file dialog and filter to XML files
            OpenFileDialog openExperimentConfigFileDialog = new OpenFileDialog {Filter = "XML files (*.xml) | *.xml"};

            if (openExperimentConfigFileDialog.ShowDialog() == true)
            {
                // Trap any errors that occur and report up to UI
                try
                {
                    // Read in experiment configuration
                    _simInstanceDirector.LoadExperimentConfiguration(openExperimentConfigFileDialog.FileName);

                    // If loading was successful, enable loading the navigator and maze genome files
                    LoadNavigatorGenomeMenuItem.IsEnabled = true;
                    LoadMazeGenomeMenuItem.IsEnabled = true;
                }
                catch (Exception expLoadException)
                {
                    MessageBox.Show(string.Format("An error occurred while loading experiment configuration: {0}",
                        expLoadException.Message), "Experiment Configuration Load Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Reset the navigator/maze genome load menu items to false
                    LoadNavigatorGenomeMenuItem.IsEnabled = false;
                    LoadMazeGenomeMenuItem.IsEnabled = false;
                }
            }
        }
    }
}