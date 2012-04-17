using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.Reactive.Subjects;
using System.Reactive;

namespace ProjectNavi.Tasks
{
    public class ActionPlayer
    {
        Game game;
        Subject<Unit> actionCompleted = new Subject<Unit>();
        IEnumerator<Action<GameTime>> actionTask;

        public ActionPlayer(Game game)
        {
            this.game = game;
        }

        public IObservable<Unit> ActionCompleted
        {
            get { return actionCompleted; }
        }

        public void PlayAction(TaskProvider task, IServiceProvider provider)
        {
            if (actionTask != null)
            {
                actionTask.Dispose();
            }

            var action = task.Action(game, provider);
            actionTask = action.GetEnumerator();
        }

        public void Update(GameTime gameTime)
        {
            if (actionTask != null)
            {
                if (actionTask.MoveNext()) actionTask.Current(gameTime);
                else
                {
                    actionCompleted.OnNext(Unit.Default);
                    actionTask = null;
                }
            }
        }
    }
}
