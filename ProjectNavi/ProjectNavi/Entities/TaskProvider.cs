using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using ProjectNavi.Hardware;

namespace ProjectNavi.Entities
{
    public abstract class TaskProvider
    {
        public string Name { get; set; }

        public abstract IEnumerable<Action<GameTime>> Action(MagabotState magabot);
    }
}
