using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Used to store data for the tiles on the grid

public class Node {
    public Vector2 pos;
    public bool occupied;
    public GameObject occupiedBy;
    public List<GameObject> island;
    public Node()
    {
        occupied = false;
        pos = new Vector2(-1000, -1000);
    }
}
