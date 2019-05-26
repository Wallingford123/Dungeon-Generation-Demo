using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;

//Main script responsible for generating all the content

public class Grid : MonoBehaviour {

    public List<GameObject> roomList;
    public GameObject corridor;
    public int gridSizeX, gridSizeY, roomNumber, maxTests, islandSizeThreshhold;
    public Node[,] nodes;
    public CharacterControl controller;

    public Line lL;

    private List<RoomData> dataList;

    private List<GameObject> rooms = new List<GameObject>();

    private List<List<GameObject>> islands = new List<List<GameObject>>();

    void Start() {
        //Initialise variables
        nodes = new Node[gridSizeX, gridSizeY];
        dataList = new List<RoomData>();
        //create grid
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                nodes[x, y] = new Node()
                {
                    pos = new Vector2(x, y)
                };
            }
        }
        int i = 0;
        //Add each room data component to a private list and assign an ID
        foreach(GameObject g in roomList)
        {
            dataList.Add(roomList[i].GetComponent<RoomData>());
            dataList[i].dat.roomID = i;
            i++;
        }
        //Generate the random rooms
        GenerateRooms();
        //Remove the walls connected to neighbours for each room
        for (int r = 0; r < rooms.Count; r++)
        {
            AdjacentDoorCreate(rooms[r].GetComponent<RoomData>());
        }
        //Unordered tree to assign islands
        AssignIslands();
        //Remove islands that do not meet size threshhold
        RemoveSmallIslands();
        //Perform Delaunay Triangulation and Minimum Spanning Tree to identify corridors, then instantiate them.
        GenerateCorridors();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            controller.SpawnAgents(lL);
        }
    }

    //Randomly places rooms without overlap
    void GenerateRooms()
    {
        //for the number of rooms specified in editor...
        for (int i = 0; i < roomNumber; i++)
        {
            //Choose a random room from prefabs
            int roomID = Random.Range(0, roomList.Count);

            //set temporary values;
            Vector2 pos = new Vector2(-1, -1);
            bool valid = false;
            int testTimes = 0;

            //attempt to make room until either one is valid or maximum tests exceeded
            while (!valid && testTimes!=maxTests)
            {
                testTimes++;
                pos = new Vector2(Random.Range(0, gridSizeX), Random.Range(0, gridSizeY));
                while (pos.x + dataList[roomID].roomWidth > gridSizeX || pos.y + dataList[roomID].roomHeight > gridSizeY)
                    pos = new Vector2(Random.Range(0, gridSizeX), Random.Range(0, gridSizeY));
                int l = 0;
                for (int x = 0; x < dataList[roomID].roomWidth; x++)
                {
                    for (int y = 0; y < dataList[roomID].roomHeight; y++)
                    {
                        if (nodes[(int)pos.x + x, (int)pos.y + y].occupied)
                        {
                            l--;
                        }
                        else l++;
                    }
                }
                if (l == dataList[roomID].roomHeight * dataList[roomID].roomWidth) valid = true;
            }

            //if valid room was made, instantiate it and update the nodes with this information
            if (testTimes != maxTests)
            {
                Vector3 correctPos = new Vector3(pos.x, 0, pos.y);
                rooms.Add(Instantiate(roomList[roomID], correctPos, Quaternion.identity));
                for (int x = 0; x < dataList[roomID].roomWidth; x++)
                {
                    for (int y = 0; y < dataList[roomID].roomHeight; y++)
                    {
                        nodes[(int)pos.x + x, (int)pos.y + y].occupied = true;
                        nodes[(int)pos.x + x, (int)pos.y + y].occupiedBy = rooms[rooms.Count-1];
                    }
                }
            }
        }
    }

    //Remove the walls connected to neighbours for each room
    public void AdjacentDoorCreate(RoomData data)
    {
        //Create object instance for walls in the room
        GameObject[] northWalls, eastWalls, southWalls, westWalls;
        northWalls = new GameObject[data.roomWidth];
        southWalls = new GameObject[data.roomWidth];
        eastWalls = new GameObject[data.roomHeight];
        westWalls = new GameObject[data.roomHeight];

        //Pass real object information to code instances based on position in hierarchy
        for (int i = 0; i < data.n.transform.childCount; i++)
        {
            northWalls[i] = data.n.transform.GetChild(i).gameObject;
        }
        for (int i = 0; i < data.e.transform.childCount; i++)
        {
            eastWalls[i] = data.e.transform.GetChild(i).gameObject;
        }
        for (int i = 0; i < data.s.transform.childCount; i++)
        {
            southWalls[i] = data.s.transform.GetChild(i).gameObject;
        }
        for (int i = 0; i < data.w.transform.childCount; i++)
        {
            westWalls[i] = data.w.transform.GetChild(i).gameObject;
        }
            
        //Checking nodes above and below the room to check if there is a neighbour, then de-activating the walls corresponding to neighbour position
        int val = -1;
        //for north and south walls...
        for (int y = 0; y < 2; y++)
        {
            //check all tiles along the width of the room
            for (int x = 0; x < data.roomWidth; x++)
            {
                //reduce the Z value by 1 so it checks tiles outside of room, and add X to move along
                int lx = (int)data.transform.position.x + x;
                int ly = (int)data.transform.position.z + val;
                //make sure it's not outside of the grid
                if (lx >= 0 && ly >= 0 && lx <= gridSizeX - 1 && ly <= gridSizeY - 1)
                {
                    //if occupied...
                    if (nodes[lx, ly].occupied)
                    {
                        //check if room is already added, adding if not
                        if (!data.dat.neighbors.Contains(nodes[lx, ly].occupiedBy))
                        {
                            data.dat.neighbors.Add(nodes[lx, ly].occupiedBy);
                        }
                        //Switch whether the north or south walls are being tested, and deactivate the wall object
                        switch (y)
                        {
                            case 0:
                                southWalls[x].SetActive(false);
                                break;
                            case 1:
                                northWalls[x].SetActive(false);
                                break;
                        }
                    }
                }
            }
            //switch to north walls after south walls complete
            val += data.roomHeight + 1;

        }
        //Checking nodes left and right of the room to check if there is a neighbour, then de-activating the walls corresponding to neighbour position
        val = -1;
        //for east and west walls...
        for (int x = 0; x < 2; x++)
        {
            //check all tiles along the height of the room
            for (int y = 0; y < data.roomHeight; y++)
            {
                //reduce the X value by 1 so it checks the tiles outside of room, and add Y to move along
                int lx = (int)data.transform.position.x + val;
                int ly = (int)data.transform.position.z + y;
                //make sure it's not outside of the grid
                if (lx >= 0 && ly >= 0 && lx <= gridSizeX - 1 && ly <= gridSizeY - 1)
                {
                    //if occupied...
                    if (nodes[lx, ly].occupied)
                    {
                        //check if room is already added, adding if not
                        if (!data.dat.neighbors.Contains(nodes[lx, ly].occupiedBy))
                        {
                            data.dat.neighbors.Add(nodes[lx, ly].occupiedBy);
                        }
                        //switch whether the east or west walls are being tested, and deactivate the wall object
                        switch (x)
                        {
                            case 0:
                                westWalls[y].SetActive(false);
                                break;
                            case 1:
                                eastWalls[y].SetActive(false);
                                break;
                        }
                    }
                }
            }
            //switch to east walls after west walls complete
            val += data.roomWidth + 1;
        }
    }

    //Use unordered tree to group neighboured rooms into islands
    void AssignIslands()
    {
        //create a list containing reference to all rooms
        List<GameObject> unassigned = new List<GameObject>();
        foreach (GameObject g in rooms) unassigned.Add(g);
        //int for number of islands
        int count = 0;
        while (unassigned.Count > 0)
        {
            //assign random colour for visualisation
            Color l = Random.ColorHSV();
            //create new island container and add it to the islands list
            List<GameObject> o = new List<GameObject>();
            islands.Add(o);
            //add next unassigned room to island and set island variable of roomdata to this island
            islands[count].Add(unassigned[0]);
            unassigned[0].GetComponent<RoomData>().dat.island = o;
            //set renderer colour for visualisation
            Renderer[] ren = unassigned[0].GetComponentsInChildren<Renderer>();
            foreach(Renderer r in ren)
            {
                r.material.color = l;
            }
            //create list to contain neighbours of selected room
            List<GameObject> nList = new List<GameObject>();
            //populate list with neighbours
            foreach (GameObject g in unassigned[0].GetComponent<RoomData>().dat.neighbors)
            {
                nList.Add(g);
            }
            //remove room from unassigned
            unassigned.Remove(unassigned[0]);
            //while there are neighbours still to be explored...
            while (nList.Count > 0)
            {
                //add the neighbours of the first room in the list
                foreach (GameObject g in nList[0].GetComponentInChildren<RoomData>().dat.neighbors)
                {
                    //check if the island already contains this room
                    if (!islands[count].Contains(g))
                    {
                        //check if the neighbour list already contains this room
                        if (!nList.Contains(g))
                        {
                            nList.Add(g);
                        }
                    }
                }
                //set renderer colour for visualisation
                Renderer[] rens = nList[0].GetComponentsInChildren<Renderer>();
                foreach (Renderer r in rens)
                {
                    r.material.color = l;
                }
                //set island variable in roomdata
                nList[0].GetComponent<RoomData>().dat.island = o;
                //add room reference to island container
                islands[count].Add(nList[0]);
                //remove room from unassigned
                unassigned.Remove(nList[0]);
                //remove room from neighbour list
                nList.Remove(nList[0]);
            }


            //island is complete, add one to total
            count++;
        }
    }

    //Removes islands below the minimum size threshhold
    void RemoveSmallIslands()
    {
        //create list of islands to be removed
        List<List<GameObject>> remList = new List<List<GameObject>>();
        //for each island...
        for (int p = 0; p < islands.Count; p++)
        {
            //check if number of rooms in island is below threshhold
            List<GameObject> l = islands[p];
            if (l.Count < islandSizeThreshhold)
            {
                //for each room in the island...
                for (int i = 0; i < l.Count; i++)
                {
                    //get room position and room information
                    Vector3 o = l[i].transform.position;
                    RoomData d = l[i].GetComponent<RoomData>();
                    //set node to be unoccupied
                    for (int x = 0; x < d.roomWidth; x++)
                    {
                        for (int y = 0; y < d.roomHeight; y++)
                        {
                            nodes[(int)o.x + x, (int)o.z + y].occupied = false;
                            nodes[(int)o.x + x, (int)o.z + y].occupiedBy = null;
                        }
                    }
                    //destroy the room gameobject
                    Destroy(l[i]);
                    //remove room reference from total room list
                    rooms.Remove(l[i]);
                }
                //add island to removal list
                remList.Add(l);
            }
        }
        //remove all reference to the islands
        while (remList.Count > 0)
        {
            islands.Remove(remList[0]);
            remList.Remove(remList[0]);
        }
    }

    //Function to generate the corridors and initialise the AI agents, after this, game is ready to run
    void GenerateCorridors()
    {
        //create the necessary lists to be used by the Delaunay Triangulation
        List<Vector2> points = new List<Vector2>();
        List<List<Vector2>> holes = new List<List<Vector2>>();
        List<int> indices = null;
        List<Vector3> vertices = null;

        //populate the points list with the midpoints of all rooms ( not islands )
        foreach (GameObject g in rooms)
        {
            RoomData d = g.GetComponent<RoomData>();
            points.Add(new Vector2(g.transform.position.x + ((float)d.roomWidth / 2), g.transform.position.z + ((float)d.roomHeight / 2)));
        }

        //triangulate
        Tri(points, holes, out indices, out vertices);

        //Create list of lines for the minimum spanning tree to output to
        List<Line> lines = null;

        //perform minimum spanning tree, outputting to the list of lines, each line containing a Vector3
        MinimumSpanningTree(indices.ToArray(), vertices, out lines);

        //check all the lines to fine the ones that span two different islands
        foreach (Line l in lines)
        {
            if (nodes[Mathf.FloorToInt(l.a.x), Mathf.FloorToInt(l.a.z)].occupiedBy.GetComponent<RoomData>().dat.island !=
                nodes[Mathf.FloorToInt(l.b.x), Mathf.FloorToInt(l.b.z)].occupiedBy.GetComponent<RoomData>().dat.island)
            {
                //create corridors between the two points in the line
                CreateCorridors(l);
            }
        }
        //remove the appropriate walls, with the list now including all the corridors
        for (int r = 0; r < rooms.Count; r++)
        {
            AdjacentDoorCreate(rooms[r].GetComponent<RoomData>());
        }

        //calculate the room closest to the origin (0,0,0)
        Line minDistLine = null;
        float minDist = 1000;
        foreach (Line l in lines)
        {
            if (Vector3.Distance(new Vector3(0, 0, 0), l.a) < minDist)
            {
                minDist = Vector3.Distance(new Vector3(0, 0, 0), l.a);
                minDistLine = l;
            }
        }
        //Tell the charactercontroller script to spawn the agents
        //controller.SpawnAgents(minDistLine);
        controller.SetCameraPosition(minDistLine.a + new Vector3(0, 20, 0));
        lL = minDistLine;
    }

    //Perform delaunay triangulation
    public static bool Tri(List<Vector2> points, List<List<Vector2>> holes, out List<int> outIndices, out List<Vector3> outVertices)
    {
        //create lists for the outputs
        outVertices = new List<Vector3>();
        outIndices = new List<int>();
        //create polygon, which is essentially a container for points
        Polygon poly = new Polygon();

        //for each of the room midpoints...
        for (int i = 0; i < points.Count; i++)
        {
            //add the vertex to the polygon
            poly.Add(new Vertex(points[i].x, points[i].y));
        }

        //create a configuration variable
        TriangleNet.Configuration p = new TriangleNet.Configuration();
        //create an incremental algorithm object
        TriangleNet.Meshing.Algorithm.Incremental l = new TriangleNet.Meshing.Algorithm.Incremental();
        //run the incremental delaunay triangulation and pass the result into a mesh
        var mesh = l.Triangulate(poly.Points, p);

        //convert mesh triangles into indices and vertices to be used by minimum spanning tree
        foreach (ITriangle t in mesh.Triangles)
        {
            //for each triangle...
            for (int j = 2; j >= 0; j--)
            {
                bool found = false;
                //for each vertex currently in the output list
                for (int k = 0; k < outVertices.Count; k++)
                {
                    //check if the current vertex matches it
                    if ((outVertices[k].x == t.GetVertex(j).x) && (outVertices[k].z == t.GetVertex(j).y))
                    {
                        //add the index value for the vertex
                        outIndices.Add(k);
                        found = true;
                        break;
                    }
                }
                //if vertex is not currently in the vertex output list
                if (!found)
                {
                    //add vertex to list and add index value
                    outVertices.Add(new Vector3((float)t.GetVertex(j).x, 0.1f, (float)t.GetVertex(j).y));
                    outIndices.Add(outVertices.Count - 1);
                }
            }
        }
        return true;
    }

    //Perform minimum spanning tree algorithm
    public void MinimumSpanningTree(int[] indices, List<Vector3> vertices, out List<Line> lines)
    {
        //create visited and unvisited lists
        List<TreeNode> visited = new List<TreeNode>();
        List<TreeNode> unvisited = new List<TreeNode>();
        //add all vertices to the unvisited list and convert them to treenodes
        foreach (Vector3 v in vertices)
        {
            unvisited.Add(new TreeNode(v));
        }
        //add connected node values to the treenodes based on the indices produced by delaunay triangulation
        for (int i = 0; i < indices.Length; i += 3)
        {
            unvisited[indices[i]].connectedNodes.Add(unvisited[indices[i + 1]]);
            unvisited[indices[i + 1]].connectedNodes.Add(unvisited[indices[i]]);

            unvisited[indices[i + 1]].connectedNodes.Add(unvisited[indices[i + 2]]);
            unvisited[indices[i + 2]].connectedNodes.Add(unvisited[indices[i + 1]]);

            unvisited[indices[i + 2]].connectedNodes.Add(unvisited[indices[i]]);
            unvisited[indices[i]].connectedNodes.Add(unvisited[indices[i + 2]]);
        }

        //move starting node to visited
        visited.Add(unvisited[0]);
        unvisited.Remove(unvisited[0]);

        //create a list of lines for output
        lines = new List<Line>();

        //while tree incomplete...
        while (unvisited.Count > 0)
        {
            //set high value that will always be modified
            float smallestWeight = 100000;
            //create empty treenodes
            TreeNode a = new TreeNode(), b = new TreeNode();
            //for each node in visited...
            foreach (TreeNode v in visited)
            {
                //for each connected node...
                foreach (TreeNode t in v.connectedNodes)
                {
                    //check if it is yet to be visited
                    if (unvisited.Contains(t))
                    {
                        //check if it is a shorter distance than the current shortest
                        if (Vector3.Distance(v.position, t.position) < smallestWeight)
                        {
                            //make reference to the two nodes that make this shortest distance
                            a = v;
                            b = t;
                            smallestWeight = Vector3.Distance(v.position, t.position);
                        }
                    }
                }
            }
            //move the connected node with the shortest distance to visited 
            visited.Add(b);
            unvisited.Remove(b);
            //create a line for output with the two relevant nodes
            lines.Add(new Line(a, b));
        }
    }

    //Instantiates the corridors
    void CreateCorridors(Line line)
    {
        //check if distance is greater on X or Y axis, defaulting to Z axis if equal
        bool lol = System.Math.Abs(line.b.x - line.a.x) > System.Math.Abs(line.b.z - line.a.z);
        //set path origin from the first node of the line
        Vector3 pos = line.a;
        switch (lol)
        {
            //if X is greater distance
            case true:
                //while the path is incomplete...
                while (Mathf.FloorToInt(pos.x) != Mathf.FloorToInt(line.b.x))
                {
                    //while movement on Z axis is not complete...
                    while (Mathf.FloorToInt(pos.z) != Mathf.FloorToInt(line.b.z))
                    {
                        //check if going north or south
                        if (pos.z > line.b.z) pos.z--;
                        else pos.z++;
                        //get reference to current node
                        Node no = nodes[(int)pos.x, (int)pos.z];
                        //check if already occupied
                        if (!no.occupied)
                        {
                            //create corridor and assign variables for the node
                            GameObject corr = Instantiate(corridor, pos, Quaternion.identity);
                            no.occupied = true;
                            no.occupiedBy = corr;
                            corr.name = "1";
                            //add corridor to the rooms list for recalculation of walls
                            rooms.Add(corr);
                        }
                    }
                    //check if going east or west
                    if (pos.x > line.b.x) pos.x--;
                    else pos.x++;
                    //get reference to current node
                    Node n = nodes[(int)pos.x, (int)pos.z];
                    //check if already occupied
                    if (!n.occupied)
                    {
                        //create corridor and assign variables for the node
                        GameObject corr = Instantiate(corridor, pos, Quaternion.identity);
                        n.occupied = true;
                        n.occupiedBy = corr;
                        corr.name = "2";
                        //add corridor to the rooms list for recalculation of walls
                        rooms.Add(corr);
                    }
                }
                break;
            //if Z is greater distance    
            case false:
                //While the path is incomplete...
                while (Mathf.FloorToInt(pos.z) != Mathf.FloorToInt(line.b.z))
                {
                    //while movement on X axis is not complete...
                    while (Mathf.FloorToInt(pos.x) != Mathf.FloorToInt(line.b.x))
                    {
                        //check if going east or west
                        if (pos.x > line.b.x) pos.x--;
                        else pos.x++;
                        //get reference to current node
                        Node n = nodes[(int)pos.x, (int)pos.z];
                        //check if already occupied
                        if (!n.occupied)
                        {
                            //create corridor and assign variables for the node
                            GameObject corr = Instantiate(corridor, pos, Quaternion.identity);
                            n.occupied = true;
                            n.occupiedBy = corr;
                            corr.name = "3";
                            //add corridor to the rooms list for recalculation of walls
                            rooms.Add(corr);
                        }
                    }
                    //check if going north or south
                    if (pos.z > line.b.z) pos.z--;
                    else pos.z++;
                    //get reference to current node
                    Node no = nodes[(int)pos.x, (int)pos.z];
                    //check if already occupied
                    if (!no.occupied)
                    {
                        //create corridor and assign variables for the node
                        GameObject corr = Instantiate(corridor, pos, Quaternion.identity);
                        corr.name = "4";
                        no.occupied = true;
                        no.occupiedBy = corr;
                        //add corridor to the rooms list for recalculation of walls
                        rooms.Add(corr);
                    }
                }
                break;
        }
    }

}

//Container for the positions of two rooms
public class Line
{
    public Vector3 a, b;
    
    //constructor for easier conversion from TreeNodes
    public Line(TreeNode aa, TreeNode bb)
    {
        a = aa.position;
        a = new Vector3(Mathf.FloorToInt(a.x), Mathf.FloorToInt(a.y), Mathf.FloorToInt(a.z));
        b = bb.position;
        b = new Vector3(Mathf.FloorToInt(b.x), Mathf.FloorToInt(b.y), Mathf.FloorToInt(b.z));
    }
}
//Container for storing data on position and connected node from minimum spanning tree
public class TreeNode
{
    public Vector3 position;
    public List<TreeNode> connectedNodes;

    public TreeNode(Vector3 pos)
    {
        position = pos;
        connectedNodes = new List<TreeNode>();
    }
    public TreeNode()
    {
        position = new Vector3(-1, -1);
        connectedNodes = null;
    }
}
