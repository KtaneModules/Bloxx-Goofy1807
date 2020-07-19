namespace Bloxx
{
    struct PathElement
    {
        public int Direction;
        public GameState State;

        public PathElement(int dir, GameState state) : this()
        {
            Direction = dir;
            State = state;
        }
    }
}
