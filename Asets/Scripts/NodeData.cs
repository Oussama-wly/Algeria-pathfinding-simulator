using System.Collections.Generic;
using UnityEngine;

public enum NodeState { Default, Start, Goal, Visited, Open, Path }

public class NodeData
{
    public int    Code;
    public string Name;
    public float  Lat;
    public float  Lon;

    public List<(int neighbor, float dist)> Neighbors = new List<(int, float)>();

    public GameObject     GameObject;
    public SpriteRenderer Renderer;
    public TextMesh       CodeLabel;
    public TextMesh       NameLabel;
}
