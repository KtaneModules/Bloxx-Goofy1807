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
    bool moduleSolved = false;
    bool horiz = false;
    bool upright = false;
    bool moveActive = false;
    bool resetActive = false;
    bool strikeActive = false;
    List<int> moves = new List<int>();

    GameObject start;
    GameObject player;
    GameObject block;

    void Start()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < Arrows.Length; i++)
        {
            Arrows[i].OnInteract += ArrowPress(i);
        }

        Reset.OnInteract += delegate ()
        {
            StartCoroutine(ResetGame());
            return false;
        };

        var cols = 15;
        var rows = 11;
        var grid = "-#O###----------#####-------------##-------------##-####--------##-########---###-####---#######-##-----##--###-##-----##------##-###-##########-X##-#----------#####";
        for (int i = 0; i < rows; i++)
        {
            var gridRow = grid.Substring(i * cols, cols);
            for (int j = 0; j < cols; j++)
            {
                if (gridRow[j].Equals('#') || gridRow[j].Equals('O') || gridRow[j].Equals('X'))
                {
                    var currentPlane = Instantiate(Plane, transform.Find("GameObjects").Find("Grid"));
                    currentPlane.transform.localPosition = new Vector3(-0.07f + (0.01f * j), currentPlane.transform.localPosition.y, 0.07f - (0.01f * i));

                    if (gridRow[j].Equals('O'))
                        start = currentPlane;
                    if (gridRow[j].Equals('X'))
                        currentPlane.GetComponent<MeshRenderer>().material.color = new Color32(0, 255, 0, 255);
                }

            }
        }
        player = Instantiate(Block, transform.Find("GameObjects"));
        player.name = "Player";
        player.transform.localPosition = new Vector3(start.transform.localPosition.x - 0.005f, start.transform.localPosition.y, start.transform.localPosition.z);
        block = player.transform.Find("Block").gameObject;
        horiz = true;
        upright = false;
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
            if (horiz)
            {
                switch (btn)
                {
                    case 0:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.005f);
                        break;
                    case 1:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.005f);
                        break;
                    case 2:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.01f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    case 3:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.01f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.01f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    default:
                        return false;
                }
            }
            else if (upright)
            {
                switch (btn)
                {
                    case 0:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.005f);
                        break;
                    case 1:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.005f);
                        break;
                    case 2:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    case 3:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    default:
                        return false;
                }
            }
            else
            {
                switch (btn)
                {
                    case 0:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.01f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z - 0.01f);
                        break;
                    case 1:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.01f);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x, block.transform.localPosition.y, block.transform.localPosition.z + 0.01f);
                        break;
                    case 2:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x + 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    case 3:
                        player.transform.localPosition = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        block.transform.localPosition = new Vector3(block.transform.localPosition.x - 0.005f, block.transform.localPosition.y, block.transform.localPosition.z);
                        break;
                    default:
                        return false;
                }
            }
            StartCoroutine(movePlayer(btn));
            return false;
        };
    }

    IEnumerator movePlayer(int btn)
    {
        moveActive = true;
        var duration = 0f;
        if (!resetActive)
            duration = .3f;
        else
            duration = .1f;
        var elapsed = 0f;
        if (horiz)
        {
            switch (btn)
            {
                case 0:
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
                    horiz = false;
                    upright = true;
                    break;
                case 3:
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
                    horiz = false;
                    upright = true;
                    break;
                default:
                    yield break;
            }
        }
        else if (upright)
        {
            switch (btn)
            {
                case 0:
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
                    upright = false;
                    horiz = false;
                    break;
                case 1:
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
                    upright = false;
                    horiz = false;
                    break;
                case 2:
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
                    upright = false;
                    horiz = true;
                    break;
                case 3:
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
                    upright = false;
                    horiz = true;
                    break;
                default:
                    yield break;
            }
        }
        else
        {
            switch (btn)
            {
                case 0:
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
                    horiz = false;
                    upright = true;
                    break;
                case 1:
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
                    horiz = false;
                    upright = true;
                    break;
                case 2:
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
                default:
                    yield break;
            }
        }
        moveActive = false;
        var cols = 15;
        var rows = 11;
        var grid = "-#O###----------#####-------------##-------------##-####--------##-########---###-####---#######-##-----##--###-##-----##------##-###-##########-X##-#----------#####";
        for (int i = 0; i < rows; i++)
        {
            var gridRow = grid.Substring(i * cols, cols);
            for (int j = 0; j < cols; j++)
            {
                if (gridRow[j].Equals('-'))
                {
                    var emptyPlaneMid = new Vector3(-0.07f + (0.01f * j), 0f, 0.07f - (0.01f * i));
                    if (player.transform.localPosition.x < -0.07f || player.transform.localPosition.x > -0.07f + (0.01f * 14) || player.transform.localPosition.z > 0.07f || player.transform.localPosition.z < 0.07f - (0.01f * 10))
                        goto Strike;
                    if (horiz)
                    {
                        var checkPlayerRight = new Vector3(player.transform.localPosition.x + 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        var checkPlayerLeft = new Vector3(player.transform.localPosition.x - 0.005f, player.transform.localPosition.y, player.transform.localPosition.z);
                        if (Mathf.Abs(checkPlayerLeft.x - emptyPlaneMid.x) < 0.001f && Mathf.Abs(checkPlayerLeft.z - emptyPlaneMid.z) < 0.001f || Mathf.Abs(checkPlayerRight.x - emptyPlaneMid.x) < 0.001f && Mathf.Abs(checkPlayerRight.z - emptyPlaneMid.z) < 0.001f)
                            goto Strike;

                    }
                    else if (upright)
                    {
                        if (Mathf.Abs(player.transform.localPosition.x - emptyPlaneMid.x) < 0.001f && Mathf.Abs(player.transform.localPosition.z - emptyPlaneMid.z) < 0.001f)
                            goto Strike;

                    }
                    else
                    {
                        var checkPlayerUp = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z + 0.005f);
                        var checkPlayerDown = new Vector3(player.transform.localPosition.x, player.transform.localPosition.y, player.transform.localPosition.z - 0.005f);
                        if (Mathf.Abs(checkPlayerUp.x - emptyPlaneMid.x) < 0.001f && Mathf.Abs(checkPlayerUp.z - emptyPlaneMid.z) < 0.001f || Mathf.Abs(checkPlayerDown.x - emptyPlaneMid.x) < 0.001f && Mathf.Abs(checkPlayerDown.z - emptyPlaneMid.z) < 0.001f)
                            goto Strike;

                    }
                }
                if (gridRow[j].Equals('X'))
                {
                    var goalMid = new Vector3(-0.07f + (0.01f * j), 0f, 0.07f - (0.01f * i));
                    if (upright && Mathf.Abs(player.transform.localPosition.x - goalMid.x) < 0.001f && Mathf.Abs(player.transform.localPosition.z - goalMid.z) < 0.001f)
                        goto Solve;
                }
            }
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
