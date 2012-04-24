using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace ProjectNavi.Entities
{
    public class LandmarkMappingCollection : KeyedCollection<int, LandmarkMapping>
    {
        protected override int GetKeyForItem(LandmarkMapping item)
        {
            return item.MarkerId;
        }
    }

    public class LandmarkMappingState : Collection<LandmarkMapping>
    {
    }

    public class LandmarkMapping
    {
        public int MarkerId { get; set; }

        public int LandmarkIndex { get; set; }
    }
}
