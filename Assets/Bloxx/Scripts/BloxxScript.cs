using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BloxxScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable[] Arrows;
    public KMSelectable Reset;
    public GameObject Plane;
    public GameObject Block;

    static int moduleIdCounter = 1;
    int moduleId;
    int cols;
    int rows;
    string grid;
    bool moduleSolved = false;
    bool moveActive = false;
    bool resetActive = false;
    bool strikeActive = false;
    readonly List<int> moves = new List<int>();

    GameObject player;
    GameObject block;
    GameState state;

    enum Orientation
    {
        Upright,
        Horiz,
        Vert
    }
    sealed class GameState
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
    }

    void Start()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < Arrows.Length; i++)
            Arrows[i].OnInteract += ArrowPress(i);

        Reset.OnInteract += delegate ()
        {
            StartCoroutine(ResetGame());
            return false;
        };

        cols = 15;
        rows = 11;
        grid = "V##########----###########----############---#############--########################################################################################################X";

        for (int i = 0; i < rows; i++)
        {
            var gridRow = grid.Substring(i * cols, cols);
            for (int j = 0; j < cols; j++)
            {
                if ("#XHVU".Contains(gridRow[j]))
                {
                    var currentPlane = Instantiate(Plane, transform.Find("GameObjects").Find("Grid"));
                    currentPlane.transform.localPosition = new Vector3(-0.07f + (0.01f * j), 0.0101f, 0.07f - (0.01f * i));
                    if (gridRow[j].Equals('X'))
                        currentPlane.GetComponent<MeshRenderer>().material.color = new Color32(0, 255, 0, 255);
                }
            }
        }

        var startPos = grid.IndexOf(ch => "HVU".Contains(ch));
        state = new GameState
        {
            curPosX = startPos % cols,
            curPosY = startPos / cols,
            orientation = grid[startPos] == 'H' ? Orientation.Horiz : grid[startPos] == 'V' ? Orientation.Vert : Orientation.Upright
        };

        player = Instantiate(Block, transform.Find("GameObjects"));
        player.name = "Player";
        block = player.transform.Find("Block").gameObject;

        switch (state.orientation)
        {
            case Orientation.Upright:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * state.curPosX), 0.0101f, 0.07f - (0.01f * state.curPosY));
                block.transform.localPosition = new Vector3(0, block.transform.localPosition.y + 0.005f, 0);
                block.transform.localEulerAngles = new Vector3(0, 0, 90);
                break;
            case Orientation.Horiz:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * state.curPosX) + 0.005f, 0.0101f, 0.07f - (0.01f * state.curPosY));
                break;
            case Orientation.Vert:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * state.curPosX), 0.0101f, 0.07f - (0.01f * state.curPosY) - 0.005f);
                block.transform.localEulerAngles = new Vector3(0, 90, 0);
                break;
        }
    }

    IEnumerator ResetGame()
    {
        resetActive = true;
        while (moves.Count > 0)
        {
            switch (moves.Last())
            {
                case 0:
                    Arrows[1].OnInteract();
                    yield return new WaitUntil(() => !moveActive);
                    break;
                case 1:
                    Arrows[0].OnInteract();
                    yield return new WaitUntil(() => !moveActive);
                    break;
                case 2:
                    Arrows[3].OnInteract();
                    yield return new WaitUntil(() => !moveActive);
                    break;
                case 3:
                    Arrows[2].OnInteract();
                    yield return new WaitUntil(() => !moveActive);
                    break;
            }
            moves.RemoveAt(moves.Count() - 1);
        }
        resetActive = false;
    }

    KMSelectable.OnInteractHandler ArrowPress(int btn)
    {
        return delegate
        {
            if (moduleSolved || moveActive || resetActive || strikeActive)
                return false;

            moves.Add(btn);
            var newState = state.Move(btn);
            StartCoroutine(movePlayer(btn, state, newState));
            state = newState;
            return false;
        };
    }

    IEnumerator movePlayer(int btn, GameState oldState, GameState newState)
    {
        moveActive = true;
        var duration = resetActive ? .1f : .3f;
        var elapsed = 0f;
        if (oldState.orientation == Orientation.Horiz)
        {
            switch (btn)
            {
                case 0:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.005f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.005f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
                case 1:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.005f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(-90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.005f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
                case 2:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.01f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, 90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.01f, block.transform.localPosition.y + 0.005f, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, 90);
                    break;
                case 3:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.01f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, -90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.01f, block.transform.localPosition.y + 0.005f, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, -90);
                    break;
            }
        }
        else if (oldState.orientation == Orientation.Upright)
        {
            switch (btn)
            {
                case 0:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.005f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.01f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y - 0.005f, block.transform.localPosition.z + 0.005f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 90, 0);
                    break;
                case 1:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.005f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(-90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.01f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y - 0.005f, block.transform.localPosition.z - 0.005f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 90, 0);
                    break;
                case 2:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, 90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y - 0.005f, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
                case 3:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, -90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y - 0.005f, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
            }
        }
        else    // Orientation.Vert
        {
            switch (btn)
            {
                case 0:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.01f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.01f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y + 0.005f, block.transform.localPosition.z + 0.01f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, 90);
                    break;
                case 1:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.01f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.01f);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(-90 * elapsed / duration, 0, 0);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y + 0.005f, block.transform.localPosition.z - 0.01f);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    block.transform.localEulerAngles = new Vector3(0, 0, 90);
                    break;
                case 2:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, 90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
                case 3:
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    while (elapsed < duration)
                    {
                        yield return null;
                        elapsed += Time.deltaTime;
                        player.transform.localEulerAngles = new Vector3(0, 0, -90 * elapsed / duration);
                    }
                    player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                    block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                    player.transform.localEulerAngles = new Vector3(0, 0, 0);
                    break;
            }
        }
        moveActive = false;

        if (newState.DeservesStrike(grid, cols))
        {
            // Ran into a wall — strike
            Module.HandleStrike();
            strikeActive = true;
            var oppositeButton = new[] { 1, 0, 3, 2 }[btn];
            yield return movePlayer(oppositeButton, newState, oldState);
            state = oldState;
            strikeActive = false;
        }
        else if (newState.IsSolved(grid, cols))
        {
            // Reached the goal position — solve
            moduleSolved = true;
            Module.HandlePass();
            duration = 1f;
            elapsed = 0f;
            var oldPos = player.transform.localPosition;
            var newPos = new Vector3(player.transform.localPosition.x, -0.011f, player.transform.localPosition.z);
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                player.transform.localPosition = Vector3.Lerp(oldPos, newPos, elapsed / duration);
            }
        }
        yield break;
    }
}
