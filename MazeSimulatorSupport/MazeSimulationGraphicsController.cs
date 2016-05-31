#region

using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using SharpNeat.Phenomes.Mazes;

#endregion

namespace MazeSimulatorSupport
{
    internal class MazeSimulationGraphicsController
    {
        private readonly int _mazeCanvasHeight;
        private readonly int _mazeCanvasWidth;
        private readonly SolidColorBrush _wallBrush;
        private readonly int _wallThickness;

        public MazeSimulationGraphicsController(int windowHeight, int windowWidth, SolidColorBrush lineBrush,
            int lineThickness)
        {
            _mazeCanvasHeight = windowHeight;
            _mazeCanvasWidth = windowWidth;
            _wallBrush = lineBrush;
            _wallThickness = lineThickness;
        }

        public List<Line> GetMazeWalls(List<MazeStructureWall> mazeStructureWalls, int mazeHeight, int mazeWidth)
        {
            // Determine scale coefficient for fitting to canvas
            double canvasHeightFitCoefficient = (double) _mazeCanvasHeight/mazeHeight;
            double canvasWidthFitCoefficient = (double) _mazeCanvasWidth/mazeWidth;

            // Add the interior walls and return the list
            return
                mazeStructureWalls.Select(
                    mazeStructureWall =>
                        CreateMazeLine(mazeStructureWall, canvasHeightFitCoefficient, canvasWidthFitCoefficient))
                    .ToList();
        }

        private Line CreateMazeLine(int xStart, int yStart, bool isHorizontal)
        {
            Line line = new Line
            {
                // Set line segment end points
                X1 = xStart,
                Y1 = yStart,
                X2 = isHorizontal ? xStart + _mazeCanvasWidth : xStart,
                Y2 = isHorizontal ? yStart : yStart + _mazeCanvasHeight,

                // Set additional layout properties
                Stroke = _wallBrush,
                StrokeThickness = _wallThickness
            };

            return line;
        }

        private Line CreateMazeLine(MazeStructureWall mazeStructureWall, double heightCoefficient,
            double widthCoefficient)
        {
            Line line = new Line
            {
                // Extract start/end points from the maze structure wall
                X1 = mazeStructureWall.StartMazeStructurePoint.X*widthCoefficient,
                Y1 = mazeStructureWall.StartMazeStructurePoint.Y*heightCoefficient,
                X2 = mazeStructureWall.EndMazeStructurePoint.X*widthCoefficient,
                Y2 = mazeStructureWall.EndMazeStructurePoint.Y*heightCoefficient,

                // Set additional layout properties
                Stroke = _wallBrush,
                StrokeThickness = _wallThickness
            };

            return line;
        }
    }
}