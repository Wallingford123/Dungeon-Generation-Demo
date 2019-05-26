using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class used to store data about the room, defined by the prefab with the exception of GenData. Is attached to the gameobjects.

public class RoomData : MonoBehaviour {

    public int roomWidth, roomHeight;

    public GenData dat = new GenData();

    public GameObject n, e, s, w;

}
