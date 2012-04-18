using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Microsoft.Xna.Framework.Content;

namespace ProjectNavi.Tasks
{
    public class SmartObject
    {
        public string Name { get; set; }

        public int MarkerId { get; set; }

        [ContentSerializer(Optional = true)]
        public TaskProvider Task { get; set; }
    }
}
