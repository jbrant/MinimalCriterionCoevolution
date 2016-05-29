using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Converters;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MazeSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Rectangle testRect;

        public MainWindow()
        {
            InitializeComponent();

            int height, width = 320;

            // Create maze walls
            Line line1 = new Line();
            line1.Stroke = Brushes.LightSteelBlue;
            line1.X1 = 0;
            line1.X2 = 700;
            line1.Y1 = 0;
            line1.Y2 = 0;
            line1.StrokeThickness = 2;

            Line line2 = new Line();
            line2.Stroke = Brushes.LightSteelBlue;
            line2.X1 = 0;
            line2.X2 = 0;
            line2.Y1 = 0;
            line2.Y2 = 700;
            line2.StrokeThickness = 2;

            Line line3 = new Line();
            line3.Stroke = Brushes.LightSteelBlue;
            line3.X1 = 700;
            line3.X2 = 700;
            line3.Y1 = 0;
            line3.Y2 = 700;
            line3.StrokeThickness = 2;

            Line line4 = new Line();
            line4.Stroke = Brushes.LightSteelBlue;
            line4.X1 = 0;
            line4.X2 = 700;
            line4.Y1 = 700;
            line4.Y2 = 700;
            line4.StrokeThickness = 2;

            SimulationCanvas.Children.Add(line1);
            SimulationCanvas.Children.Add(line2);
            SimulationCanvas.Children.Add(line3);
            SimulationCanvas.Children.Add(line4);

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
            Canvas.SetLeft(testRect, Canvas.GetLeft(testRect)+1);
        }

        private void OpenMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
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
    }
}
