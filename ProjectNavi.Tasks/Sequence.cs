using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ProjectNavi.Tasks
{
    public class Sequence : TaskProvider
    {
        readonly List<TaskProvider> tasks = new List<TaskProvider>();

        public List<TaskProvider> Tasks
        {
            get { return tasks; }
        }

        public override IEnumerable<Action<GameTime>> Action(Game game, IServiceProvider provider)
        {
            foreach (var task in tasks)
            {
                foreach (var step in task.Action(game, provider))
                {
                    yield return step;
                }
            }
        }
    }
}
