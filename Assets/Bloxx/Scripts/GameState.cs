using System;

namespace Bloxx
{
    sealed class GameState : IEquatable<GameState>
    {
        public int curPosX;
        public int curPosY;
        public Orientation orientation;

        public GameState Move(int direction)
        {
            var newState = new GameState { curPosX = curPosX, curPosY = curPosY, orientation = orientation };
            newState.moveImpl(direction);
            return newState;
        }

        private void moveImpl(int direction)
        {
            switch (orientation)
            {
                case Orientation.Upright:
                    switch (direction)
                    {
                        case 0: curPosY -= 2; orientation = Orientation.Vert; break;
                        case 1: curPosY++; orientation = Orientation.Vert; break;
                        case 2: curPosX -= 2; orientation = Orientation.Horiz; break;
                        case 3: curPosX++; orientation = Orientation.Horiz; break;
                    }
                    break;
                case Orientation.Horiz:
                    switch (direction)
                    {
                        case 0: curPosY--; break;
                        case 1: curPosY++; break;
                        case 2: curPosX--; orientation = Orientation.Upright; break;
                        case 3: curPosX += 2; orientation = Orientation.Upright; break;
                    }
                    break;
                case Orientation.Vert:
                    switch (direction)
                    {
                        case 0: curPosY--; orientation = Orientation.Upright; break;
                        case 1: curPosY += 2; orientation = Orientation.Upright; break;
                        case 2: curPosX--; break;
                        case 3: curPosX++; break;
                    }
                    break;
            }
        }

        public bool DeservesStrike(string grid, int cols)
        {
            var rows = grid.Length / cols;
            return curPosX < 0 || curPosX >= cols || curPosY < 0 || curPosY >= rows || grid[curPosX + cols * curPosY] == '-' ||
                        (orientation == Orientation.Horiz && (curPosX >= cols - 1 || grid[curPosX + 1 + cols * curPosY] == '-')) ||
                        (orientation == Orientation.Vert && (curPosY >= rows - 1 || grid[curPosX + cols * (curPosY + 1)] == '-'));
        }

        public bool IsSolved(string grid, int cols)
        {
            return orientation == Orientation.Upright && grid[curPosX + cols * curPosY] == 'X';
        }

        public bool Equals(GameState other) { return other != null && other.curPosX == curPosX && other.curPosY == curPosY && other.orientation == orientation; }
        public override bool Equals(object obj) { return obj is GameState && Equals((GameState) obj); }
        public override int GetHashCode() { return unchecked(curPosX * 101 + curPosY * 73 + (int) orientation); }

        public void MarkUsed(char[] newGrid, int cols, char ch = '#')
        {
            newGrid[curPosX + cols * curPosY] = ch;
            switch (orientation)
            {
                case Orientation.Horiz: newGrid[curPosX + 1 + cols * curPosY] = ch; break;
                case Orientation.Vert: newGrid[curPosX + cols * (curPosY + 1)] = ch; break;
            }
        }

        public char posChar()
        {
            switch (orientation)
            {
                case Orientation.Upright: return 'U';
                case Orientation.Horiz: return 'H';
                case Orientation.Vert: return 'V';
                default: throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}/{2}", curPosX, curPosY, orientation);
        }
    }
}
