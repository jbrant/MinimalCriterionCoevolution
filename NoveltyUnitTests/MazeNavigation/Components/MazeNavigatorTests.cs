#region

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace SharpNeat.Domains.MazeNavigation.Components.Tests
{
    [TestClass]
    public class MazeNavigatorTests
    {
        [TestMethod]
        public void MazeNavigatorTest()
        {
            //Assert.Fail();
        }

        [TestMethod]
        public void MoveTest()
        {
            List<Wall> walls = new List<Wall>(11)
            {
                // Boundary walls
                new Wall(new DoubleLine(7, 202, 195, 198), -1, -1, 5),
                new Wall(new DoubleLine(41, 5, 3, 8), -1, -1, 5),
                new Wall(new DoubleLine(3, 8, 4, 49), 1, 1, 5),
                new Wall(new DoubleLine(4, 49, 7, 202), 1, 1, 5),
                new Wall(new DoubleLine(195, 198, 186, 8), -1, -1, 5),
                new Wall(new DoubleLine(186, 8, 39, 5), -1, -1, 5),

                // Obstructing walls
                new Wall(new DoubleLine(4, 49, 57, 53), 1, 1, 5),
                new Wall(new DoubleLine(56, 54, 56, 157), -1, 1, 5),
                new Wall(new DoubleLine(57, 106, 158, 162), 1, 1, 5),
                new Wall(new DoubleLine(77, 201, 108, 164), -1, -1, 5),
                new Wall(new DoubleLine(6, 80, 33, 121), -1, 1, 5),
                new Wall(new DoubleLine(192, 146, 87, 91), -1, -1, 5),
                new Wall(new DoubleLine(56, 55, 133, 30), 1, 1, 5)
            };

            //DoublePoint goalLocation = new DoublePoint(270, 100);
            DoublePoint goalLocation = new DoublePoint(31, 20);

            //MazeNavigator navigator = new MazeNavigator(new DoublePoint(30, 22));
            MazeNavigator navigator = new MazeNavigator(new DoublePoint(14.8669777, 190.702774));

            navigator.Speed = -3;
            navigator.AngularVelocity = -3;
            navigator.Heading = 325.075531;

            navigator.Move(walls, goalLocation);

            Console.WriteLine(DoublePoint.CalculateEuclideanDistance(navigator.Location, goalLocation));
        }
    }
}