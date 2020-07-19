using System.Collections.Generic;
using System.Linq;
using RT.Dijkstra;

namespace Bloxx
{
    sealed class BloxxNode : Node<int, PathElement>
    {
        public GameState GameState;
        public GameState DesiredEndState;
        public HashSet<GameState> ValidStates;
        public string ValidPositions;
        public int ValidPositionsWidth;

        public override bool IsFinal { get { return GameState.Equals(DesiredEndState); } }

        public override IEnumerable<Edge<int, PathElement>> Edges
        {
            get
            {
                return Enumerable.Range(0, 4)
                    .Select((dir, i) => new { Dir = dir, State = GameState.Move(i) })
                    .Where(inf => ValidStates != null ? ValidStates.Contains(inf.State) : !inf.State.DeservesStrike(ValidPositions, ValidPositionsWidth))
                    .Select(inf => new Edge<int, PathElement>(1, new PathElement(inf.Dir, inf.State), new BloxxNode { GameState = inf.State, DesiredEndState = DesiredEndState, ValidStates = ValidStates, ValidPositions = ValidPositions, ValidPositionsWidth = ValidPositionsWidth }));
            }
        }

        public override bool Equals(Node<int, PathElement> other) { return other is BloxxNode && ((BloxxNode) other).GameState.Equals(GameState); }
        public override int GetHashCode() { return GameState.GetHashCode(); }
    }
}