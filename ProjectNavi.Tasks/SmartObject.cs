using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace ProjectNavi.Tasks
{
    public class SmartObject
    {
        public string Name { get; set; }

        public int MarkerId { get; set; }

        public TaskProvider Task { get; set; }
    }
}
