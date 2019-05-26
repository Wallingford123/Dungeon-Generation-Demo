using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Script for controlling the camera and the AI agents

public class CharacterControl : MonoBehaviour
{
    public GameObject tracker, cam, characterPrefab;
    public float cameraMoveSpeed;
    public int characterNumber;
    public LocalNavMeshBuilder builder;

    public List<GameObject> characterInstances;
    private int leadingAgent;
    private float lastCheck, yPos;

    void Start()
    {
        //initialise variables
        lastCheck = Time.time;
        yPos = 20;
        leadingAgent = 0;
    }

    void FixedUpdate()
    {
        //if there are agents in the scene...
        if (characterInstances.Count > 0)
        {
            //move the camera to follow the leading agent
            cam.transform.position = Vector3.Lerp(cam.transform.position, characterInstances[leadingAgent].transform.position + new Vector3(0, yPos, 0), cameraMoveSpeed);
            //check user clicks left mouse button to set target position
            if (Input.GetMouseButtonDown(0))
            {
                GetClick();
            }
            //check scroll wheel input to zoom in and out
            yPos -= Input.mouseScrollDelta.y*2;
            //do not allow user to zoom too far or too close
            if (yPos < 10) yPos = 10;
            if (yPos > 200) yPos = 200;
            //to avoid constant camera target changes immediately after new target set, only perform leader check every second
            if (Time.time > lastCheck + 1f)
            {
                //create variable for distance tracking
                double d = -1;
                //for each agent...
                foreach (GameObject g in characterInstances)
                {
                    //get reference to the navigation script
                    UnityEngine.AI.NavMeshAgent agent = g.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    //if not first agent
                    if (d != -1)
                    {
                        //check if agent is active
                        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
                        {
                            //calculate the remaining distance
                            float dis = CalulatePathDistance(agent.path);
                            //if this is the new shortest path...
                            if (dis < d)
                            {
                                //set leading agent to the index of this agent
                                leadingAgent = characterInstances.IndexOf(g);
                                //set distance to this agent's remaining distance
                                d = dis;
                            }
                        }
                    }
                    //if first agent
                    else
                    {
                        //check if agent is active
                        if (agent.isOnNavMesh && agent.isActiveAndEnabled)
                        {
                            //set leading agent to 0 (this agent)
                            leadingAgent = 0;
                            //set distance to this agent's remaining distance
                            d = CalulatePathDistance(agent.path);
                        }
                    }
                }
                //set the last check to the current time, so it doesn't check again for another second
                lastCheck = Time.time;
            }
            //if middle mouse button pressed, reset camera zoom
            if (Input.GetMouseButtonDown(2)) yPos = 20;
            //if right mouse button pressed, cancel the navigation, setting the target to the leading agent's current position
            if (Input.GetMouseButtonDown(1)) tracker.transform.position = characterInstances[leadingAgent].transform.position;
        }
        
    }

    //calculate the total path distance left to travel for an agent
    float CalulatePathDistance(UnityEngine.AI.NavMeshPath path)
    {
        float distance = 0;
        //for each node...
        for(int i = 0; i < path.corners.Length -1; i++)
        {
            //get the distance between it and the next one and add it to total
            distance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
        }
        //return total distance
        return distance;
    }


    //Convert a player's mouse click into a position
    void GetClick()
    {
        //reset leading agent
        leadingAgent = 0;
        RaycastHit hit;
        //raycasts from mouse position relative to the camera (so what it looks like you click on is what you click on)
        if (Physics.Raycast(cam.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition), out hit, 10000))
        {
            //sets the tracker to the click point
            tracker.transform.position = hit.point;
        }
        //forces camera to recheck target
        lastCheck = Time.time - 1f;
    }

    //spawn agents and set to active
    public void SpawnAgents(Line minDistLine)
    {
        if (!builder.isActiveAndEnabled)
        {
            builder.enabled = true;
            tracker.transform.position = minDistLine.a + new Vector3(1, 0, 1);
        }
        //for each character...
        for (int i = 0; i < characterNumber; i++)
        {

            GameObject l;
            //instantiates the agent
            l = Instantiate(characterPrefab, minDistLine.a + new Vector3(i * 0.1f, 1, i * 0.1f), Quaternion.identity);
            //sets the destination target to the tracker
            l.GetComponent<UnityStandardAssets.Characters.ThirdPerson.AICharacterControl>().SetTarget(tracker.transform);
            //activates the object
            l.SetActive(true);
            //adds it to character list
            characterInstances.Add(l);
        }
        //move tracker slightly to avoid agents being propelled vertically as a result
    }

    public void SetCameraPosition(Vector3 pos)
    {
        cam.transform.position = pos;
    }
}
