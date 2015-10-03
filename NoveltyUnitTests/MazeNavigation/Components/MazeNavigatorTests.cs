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
                new DoubleLine(293, 7, 289, 130),
                new DoubleLine(289, 130, 6, 134),
                new DoubleLine(6, 134, 8, 5),
                new DoubleLine(8, 5, 292, 7),
                new DoubleLine(241, 130, 58, 65),
                new DoubleLine(114, 7, 73, 42),
                new DoubleLine(130, 91, 107, 46),
                new DoubleLine(196, 8, 139, 51),
                new DoubleLine(219, 122, 182, 63),
                new DoubleLine(267, 9, 214, 63),
                new DoubleLine(271, 129, 237, 88)
            };

            DoublePoint goalLocation = new DoublePoint(270, 100);

            MazeNavigator navigator = new MazeNavigator(new DoublePoint(30, 22));

            navigator.Speed = 0.00406849384;
            navigator.AngularVelocity = 0.0138337612;
            navigator.Heading = 0;

            navigator.Move(walls, goalLocation);

            Console.WriteLine(DoublePoint.CalculateEuclideanDistance(navigator.Location, goalLocation));
        }
    }
}