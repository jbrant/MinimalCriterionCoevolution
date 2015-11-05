using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpNeat.Domains.MazeNavigation.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.MazeNavigation.Components.Tests
{
    [TestClass()]
    public class MazeNavigatorTests
    {
        [TestMethod()]
        public void MazeNavigatorTest()
        {
            //Assert.Fail();
        }

        [TestMethod()]
        public void MoveTest()
        {
            List<DoubleLine> walls = new List<DoubleLine>(11)
            {
                /*new DoubleLine(293, 7, 289, 130),
                new DoubleLine(289, 130, 6, 134),
                new DoubleLine(6, 134, 8, 5),
                new DoubleLine(8, 5, 292, 7),
                new DoubleLine(241, 130, 58, 65),
                new DoubleLine(114, 7, 73, 42),
                new DoubleLine(130, 91, 107, 46),
                new DoubleLine(196, 8, 139, 51),
                new DoubleLine(219, 122, 182, 63),
                new DoubleLine(267, 9, 214, 63),
                new DoubleLine(271, 129, 237, 88)*/

                new DoubleLine(41, 5, 3, 8),
                        new DoubleLine(3, 8, 4, 49),
                        new DoubleLine(4, 49, 57, 53),
                        new DoubleLine(4, 49, 7, 202),
                        new DoubleLine(7, 202, 195, 198),
                        new DoubleLine(195, 198, 186, 8),
                        new DoubleLine(186, 8, 39, 5),
                        new DoubleLine(56, 54, 56, 157),
                        new DoubleLine(57, 106, 158, 162),
                        new DoubleLine(77, 201, 108, 164),
                        new DoubleLine(6, 80, 33, 121),
                        new DoubleLine(192, 146, 87, 91),
                        new DoubleLine(56, 55, 133, 30)
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