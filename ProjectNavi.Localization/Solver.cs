using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectNavi.Localization
{
    public struct Transition<TState, TCost>
    {
        private readonly TState state;
        private readonly TCost cost;

        public Transition(TState state, TCost cost)
        {
            this.state = state;
            this.cost = cost;
        }

        public TState State
        {
            get { return this.state; }
        }

        public TCost Cost
        {
            get { return this.cost; }
        }
    }

    public static class Solver
    {
        private class PathElement<TState>
        {
            public PathElement<TState> Parent { get; set; }

            public TState State { get; set; }
        }

        public static IEnumerable<TState> BestPath<TState, TCost>(TState start, Func<TState, bool> goal, Func<TState, IEnumerable<Transition<TState, TCost>>> successors, Func<TState, TCost> estimate, IEqualityComparer<TState> stateComparer, IComparer<TCost> costComparer, Func<TCost, TCost, TCost> costAddition)
        {
            var closed = new HashSet<TState>(stateComparer);
            var open = new PriorityCollection<TCost, PathElement<TState>>(costComparer);
            open.Enqueue(new PathElement<TState> { State = start }, default(TCost));
            while (open.Count > 0)
            {
                TCost nodeCost;
                var node = open.Dequeue(out nodeCost);
                var state = node.State;
                if (goal(state))
                {
                    do
                    {
                        yield return node.State;
                        node = node.Parent;
                    }
                    while (node.Parent != null);
                    yield break;
                }
                if (!closed.Add(state)) continue;
                foreach (var successor in successors(state))
                {
                    TCost successorCost = costAddition(costAddition(nodeCost, successor.Cost), estimate(successor.State));
                    var element = new PathElement<TState> { Parent = node, State = successor.State };
                    open.Enqueue(element, successorCost);
                }
            }
            yield break;
        }
    }
}
