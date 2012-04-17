using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Tasks
{
    public class NavigationEnvironment
    {
        readonly List<SmartObject> landmarks = new List<SmartObject>();

        public List<SmartObject> Landmarks
        {
            get { return landmarks; }
        }
    }
}
