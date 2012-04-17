using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Tasks
{
    public abstract class TaskProvider
    {
        public abstract IEnumerable<Action<GameTime>> Action(Game game, IServiceProvider provider);
    }
}
