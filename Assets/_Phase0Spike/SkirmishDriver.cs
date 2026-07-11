using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Phase-0 throwaway: scripts a simple 3v3 skirmish for the toon-ink combat look test.
// Players (under "Players") advance +X, Enemies (under "Enemies") advance -X, they clash,
// enemies take casualties (Die), players push forward. Not the real Core sim.
public class SkirmishDriver : MonoBehaviour
{
    public float walkSpeed = 1.15f;
    public float advanceTime = 2.2f;
    public float clashGap = 1.6f;

    readonly List<Transform> players = new List<Transform>();
    readonly List<Transform> enemies = new List<Transform>();

    void Collect(string parent, List<Transform> into)
    {
        var p = GameObject.Find(parent);
        if (p == null) return;
        foreach (Transform c in p.transform) into.Add(c);
    }

    IEnumerator Start()
    {
        Collect("Players", players);
        Collect("Enemies", enemies);
        yield return new WaitForSeconds(0.6f);

        SetMoving(players, true); SetMoving(enemies, true);
        float t = 0f;
        while (t < advanceTime)
        {
            t += Time.deltaTime;
            Move(players, +1f); Move(enemies, -1f);
            yield return null;
        }
        SetMoving(players, false); SetMoving(enemies, false);
        yield return new WaitForSeconds(0.8f);

        // volley: enemy casualties fall one by one
        foreach (var e in enemies)
        {
            var a = e.GetComponentInChildren<Animator>();
            if (a) a.SetTrigger("Die");
            yield return new WaitForSeconds(0.45f);
        }
        yield return new WaitForSeconds(1.1f);

        // players push forward over the ground they won
        SetMoving(players, true);
        t = 0f;
        while (t < 1.2f) { t += Time.deltaTime; Move(players, +1f); yield return null; }
        SetMoving(players, false);
    }

    void SetMoving(List<Transform> list, bool v)
    {
        foreach (var u in list)
        {
            var a = u.GetComponentInChildren<Animator>();
            if (a) a.SetBool("Moving", v);
        }
    }

    void Move(List<Transform> list, float dir)
    {
        float d = dir * walkSpeed * Time.deltaTime;
        foreach (var u in list)
            if (u) u.position += new Vector3(d, 0f, 0f);
    }
}
