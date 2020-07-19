using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Bloxx;
using RT.Dijkstra;
using UnityEngine;

using Rnd = UnityEngine.Random;

public partial class BloxxScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMSelectable[] Arrows;
    public KMSelectable Reset;
    public GameObject Plane;
    public GameObject Block;

    const int cols = 15;
    const int rows = 11;
    const int numCheckPoints = 6;
    const string validPositions = "###########----###########----############---#############--#########################################################################################################";
    string grid;
    bool moduleSolved = false;
    bool moveActive = false;
    bool resetActive = false;
    bool strikeActive = false;
    bool threadReady = false;
    PathElement[] solution; // used only by the TP autosolver
    readonly List<int> moves = new List<int>();
    static int moduleIdCounter = 1;
    int moduleId;
    static readonly int[] oppositeButtons = new[] { 1, 0, 3, 2 };

    GameObject player;
    GameObject block;
    GameState state;

    void Start()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < Arrows.Length; i++)
            Arrows[i].OnInteract += ArrowPress(i);

        Reset.OnInteract += delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Reset.transform);
            Reset.AddInteractionPunch();
            if (!threadReady || moduleSolved || moveActive || resetActive || strikeActive)
                return false;

            StartCoroutine(ResetGame());
            return false;
        };

        var seed = Rnd.Range(0, int.MaxValue);
        var thread = new Thread(() => LevelGenerator(seed));
        thread.Start();
        StartCoroutine(waitForThread());
    }

    IEnumerator waitForThread()
    {
        while (!threadReady)
            yield return null;

        Debug.LogFormat("[Bloxx #{0}] Grid:\n{1}", moduleId, Enumerable.Range(0, rows).Select(row => grid.Substring(cols * row, cols)).Join("\n"));
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

    void LevelGenerator(int seed)
    {
        var rnd = new System.Random(seed);
        int unusedVariable;

        startOver:
        var validStates = new List<GameState>();
        for (var x = 0; x < cols; x++)
            for (var y = 0; y < rows; y++)
                foreach (var or in new[] { Orientation.Horiz, Orientation.Vert, Orientation.Upright })
                {
                    var state = new GameState { curPosX = x, curPosY = y, orientation = or };
                    if (!state.DeservesStrike(validPositions, cols))
                        validStates.Add(state);
                }

        var checkPoints = new List<GameState>();
        var startChPtIx = rnd.Next(0, validStates.Count);
        checkPoints.Add(validStates[startChPtIx]);
        validStates.RemoveAt(startChPtIx);
        var newGrid = new char[validPositions.Length];
        for (var i = 0; i < newGrid.Length; i++)
            newGrid[i] = '-';

        for (var i = 1; i < numCheckPoints; i++)
        {
            tryAgain:
            var ix = rnd.Next(0, validStates.Count);
            if (i == numCheckPoints - 1 && validStates[ix].orientation != Orientation.Upright)
                goto tryAgain;
            if (i > 0 && ManhattanDistance(checkPoints.Last(), validStates[ix]) < 7)
                goto tryAgain;
            var nextCheckPoint = validStates[ix];

            try
            {
                var node = new BloxxNode { ValidStates = new HashSet<GameState>(validStates), GameState = checkPoints.Last(), DesiredEndState = nextCheckPoint };
                var path = DijkstrasAlgorithm.Run(node, 0, (a, b) => a + b, out unusedVariable);
                foreach (var tup in path)
                {
                    tup.State.MarkUsed(newGrid, cols);
                    validStates.Remove(tup.State);
                }
                checkPoints.Add(nextCheckPoint);
            }
            catch (DijkstraNoSolutionException<int, PathElement>)
            {
                goto startOver;
            }
            validStates.Remove(nextCheckPoint);
        }

        // Find shortest path
        var overallStart = new BloxxNode { GameState = checkPoints[0], DesiredEndState = checkPoints.Last(), ValidPositions = new string(newGrid), ValidPositionsWidth = cols };
        var shortestPath = DijkstrasAlgorithm.Run(overallStart, 0, (a, b) => a + b, out unusedVariable).ToArray();

        var finalGrid = new char[validPositions.Length];
        for (var i = 0; i < finalGrid.Length; i++)
            finalGrid[i] = '-';
        // Mark reachable squares
        for (var spIx = 0; spIx < shortestPath.Length; spIx++)
            shortestPath[spIx].State.MarkUsed(finalGrid, cols, spIx == shortestPath.Length - 1 ? 'X' : '#');
        // Mark start and end location
        checkPoints[0].MarkUsed(finalGrid, cols, checkPoints[0].posChar());
        checkPoints.Last().MarkUsed(finalGrid, cols, 'X');
        if (finalGrid.Count(ch => ch == '#') < 40)
            goto startOver;

        state = checkPoints[0];
        grid = new string(finalGrid);
        solution = shortestPath;
        threadReady = true;
    }

    static int ManhattanDistance(GameState state1, GameState state2)
    {
        return Math.Abs(state1.curPosX - state2.curPosX) + Math.Abs(state1.curPosY - state2.curPosY);
    }

    IEnumerator ResetGame()
    {
        resetActive = true;
        while (moves.Count > 0)
        {
            var dir = oppositeButtons[moves.Last()];
            var prevState = state.Move(dir);
            yield return movePlayer(dir, state, prevState);
            state = prevState;
            moves.RemoveAt(moves.Count - 1);
        }
        resetActive = false;
    }

    KMSelectable.OnInteractHandler ArrowPress(int btn)
    {
        return delegate
        {
            if (!threadReady || moduleSolved || moveActive || resetActive || strikeActive)
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
            moves.RemoveAt(moves.Count - 1);
            strikeActive = true;
            yield return movePlayer(oppositeButtons[btn], newState, oldState);
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
            var newPos = new Vector3(player.transform.localPosition.x, -0.02f, player.transform.localPosition.z);
            while (elapsed < duration)
            {
                yield return null;
                elapsed += Time.deltaTime;
                player.transform.localPosition = Vector3.Lerp(oldPos, newPos, elapsed / duration);
            }
        }
        yield break;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} ULLURDD [move in the specified directions] | !{0} reset";
#pragma warning restore 414

    public IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*([udlr ,;]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            yield return null;
            foreach (var ch in m.Groups[1].Value)
            {
                switch (ch)
                {
                    case 'u': case 'U': Arrows[0].OnInteract(); break;
                    case 'd': case 'D': Arrows[1].OnInteract(); break;
                    case 'l': case 'L': Arrows[2].OnInteract(); break;
                    case 'r': case 'R': Arrows[3].OnInteract(); break;
                }
                while (moveActive)
                    yield return null;
            }
        }
        else if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            yield return new[] { Reset };
        }
    }

    public IEnumerator TwitchHandleForcedSolve()
    {
        while (!threadReady || moveActive || resetActive || strikeActive)
            yield return true;

        if (moduleSolved)
            yield break;

        yield return null;  // It is possible for this coroutine to continue before waitForThread() gets a chance to create the gameObjects

        int solutionIx = -1;
        while (moves.Count > 0 && (solutionIx = solution.IndexOf(pe => pe.State.Equals(state))) == -1)
        {
            // Undo a move
            Arrows[oppositeButtons[moves.Last()]].OnInteract();
            while (moveActive)
                yield return true;
            moves.RemoveRange(moves.Count - 2, 2);
        }

        for (var i = solutionIx + 1; i < solution.Length; i++)
        {
            Arrows[solution[i].Direction].OnInteract();
            while (moveActive)
                yield return true;
        }

        while (!moduleSolved)
            yield return true;
    }
}
