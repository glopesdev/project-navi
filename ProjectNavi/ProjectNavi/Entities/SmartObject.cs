using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace ProjectNavi.Entities
{
    public class SmartObject
    {
        Collection<TaskProvider> tasks = new Collection<TaskProvider>();

        public string Name { get; set; }

        public Collection<TaskProvider> Tasks
        {
            get { return tasks; }
        }
    }
}
