using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NavigationEngine
{
    abstract class SimulatorObject
    {
        public string name;
        public Point2D location; //location
        public bool dynamic; //will this object ever move?
        public bool visible; //is this object active
        public bool selected; //is this object selected (used for visualization)
        public abstract void update();
        public abstract void undo();
    }
}
