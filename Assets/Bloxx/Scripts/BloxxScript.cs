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
    List<int> moves = new List<int>();

    int curPosX;
    int curPosY;
    Orientation orientation;
    enum Orientation
    {
        Upright,
        Horiz,
        Vert
    }

    GameObject player;
    GameObject block;

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
        curPosX = startPos % cols;
        curPosY = startPos / cols;
        orientation = grid[startPos] == 'H' ? Orientation.Horiz : grid[startPos] == 'V' ? Orientation.Vert : Orientation.Upright;

        player = Instantiate(Block, transform.Find("GameObjects"));
        player.name = "Player";
        block = player.transform.Find("Block").gameObject;
        switch (orientation)
        {
            case Orientation.Upright:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * curPosX), 0.0101f, 0.07f - (0.01f * curPosY));
                block.transform.localPosition = new Vector3(0, block.transform.localPosition.y + 0.005f, 0);
                block.transform.localEulerAngles = new Vector3(0, 0, 90);
                break;
            case Orientation.Horiz:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * curPosX) + 0.005f, 0.0101f, 0.07f - (0.01f * curPosY));
                break;
            case Orientation.Vert:
                player.transform.localPosition = new Vector3(-0.07f + (0.01f * curPosX), 0.0101f, 0.07f - (0.01f * curPosY) - 0.005f);
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
                default:
                    yield break;
            }
            moves.RemoveAt(moves.Count() - 1);
        }
        resetActive = false;
    }

    KMSelectable.OnInteractHandler ArrowPress(int btn)
    {
        return delegate
        {
            if (moduleSolved || moveActive)
                return false;
            if (!resetActive && !strikeActive)
                moves.Add(btn);

            StartCoroutine(movePlayer(btn));
            return false;
        };
    }

    IEnumerator movePlayer(int btn)
    {
        moveActive = true;
        var duration = resetActive ? .1f : .3f;
        var elapsed = 0f;
        if (orientation == Orientation.Horiz)
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
                    curPosY--;
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
                    curPosY++;
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
                    curPosX--;
                    orientation = Orientation.Upright;
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
                    curPosX += 2;
                    orientation = Orientation.Upright;
                    break;
                default:
                    yield break;
            }
        }
        else if (orientation == Orientation.Upright)
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
                    curPosY -= 2;
                    orientation = Orientation.Vert;
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
                    curPosY++;
                    orientation = Orientation.Vert;
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
                    curPosX -= 2;
                    orientation = Orientation.Horiz;
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
                    curPosX++;
                    orientation = Orientation.Horiz;
                    break;
                default:
                    yield break;
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
                    curPosY--;
                    orientation = Orientation.Upright;
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
                    curPosY += 2;
                    orientation = Orientation.Upright;
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
                    curPosX--;
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
                    curPosX++;
                    break;
                default:
                    yield break;
            }
        }
        moveActive = false;

        if (curPosX < 0 || curPosX >= cols || curPosY < 0 || curPosY >= rows || grid[curPosX + cols * curPosY] == '-')
            goto Strike;

        switch (orientation)
        {
            case Orientation.Upright:
                if (grid[curPosX + cols * curPosY] == 'X')
                    goto Solve;
                break;

            case Orientation.Horiz:
                if (curPosX >= cols - 1 || grid[curPosX + 1 + cols * curPosY] == '-')
                    goto Strike;
                break;

            case Orientation.Vert:
                if (curPosY >= rows - 1 || grid[curPosX + cols * (curPosY + 1)] == '-')
                    goto Strike;
                break;
        }
        yield break;

        Strike:
        Module.HandleStrike();
        strikeActive = true;
        moves.RemoveAt(moves.Count() - 1);
        switch (btn)
        {
            case 0:
                Arrows[1].OnInteract();
                break;
            case 1:
                Arrows[0].OnInteract();
                break;
            case 2:
                Arrows[3].OnInteract();
                break;
            case 3:
                Arrows[2].OnInteract();
                break;
            default:
                yield break;
        }
        strikeActive = false;
        yield break;

        Solve:
        moduleSolved = true;
        Module.HandlePass();
        var solveDuration = 1f;
        var solveElapsed = 0f;
        while (solveElapsed < solveDuration)
        {
            yield return null;
            solveElapsed += Time.deltaTime;
            player.transform.localPosition = Vector3.Lerp(new Vector3(player.transform.localPosition.x, player.transform.localPosition.y - 0.0005f, player.transform.localPosition.z), player.transform.localPosition, solveElapsed / solveDuration);
        }
        yield break;
    }
}
