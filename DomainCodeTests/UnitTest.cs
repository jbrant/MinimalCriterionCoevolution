using System;
using Windows.UI.ViewManagement;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using SharpNeat.Domains.MazeNavigation.Components;

namespace DomainCodeTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            MazeNavigator navigator = new MazeNavigator();
        }
    }
}
