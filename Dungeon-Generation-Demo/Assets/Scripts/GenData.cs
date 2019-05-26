using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Stores data relevant to generation

public class GenData{

    public int roomID;

    public List<GameObject> neighbors, island;


    public GenData()
    {
        roomID = -1;
        neighbors = new List<GameObject>();
    }
}
