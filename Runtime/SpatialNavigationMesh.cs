using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Octant
{
    public Vector3 position;
    public float size;

    public bool isLeaf; // Checks if the octant is a leaf
    public bool isClear; // Checks if the octant is navigable
    public bool isTransitable; // Checks if there is a child that is clear

    public int layer;
    public int parentIdx;
    public int firstChildIdx;

    // Neighbours
    public int neighbourPosX;
    public int neighbourNegX;

    public int neighbourPosY;
    public int neighbourNegY;

    public int neighbourPosZ;
    public int neighbourNegZ;
}

public class SpatialNavigationMesh : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField][Min(0.1f)] private float rootSize = 20f;
    [SerializeField][Range(0, 6)] private int maxLayer = 4;
    [SerializeField] private LayerMask obstacleLayerMask = Physics.DefaultRaycastLayers;

    private float epsilon = 0.01f;

    [Header("-------------------------------- Debug --------------------------------\n")]
    [SerializeField] private bool debugMode;
    [SerializeField] private bool verboseMode;

    [Header("Octants")]
    [SerializeField] private bool showCollidingOctants;
    [SerializeField] private bool showFreeOctants;
    [SerializeField] private bool showNeighbours;

    [Header("Focus Octant")]
    [SerializeField] private int focusedOctant;
    public int focusedOctantParent;
    public bool showParent;
    public bool showChildren;
    [SerializeField] private bool showAllOctants;
    public List<int> neighbours;
    public List<int> children;
    private HashSet<int> visitedOctants;

    [Header("Internal Variables")]
    private List<Octant> octree;
    private int layerCount;

    [Header("To Delete")]
    private int emergencyExit = 10000;

    public bool IsBuilt { get { return octree != null && octree.Count > 0; } }

    private void Start() { if (!IsBuilt) { Build(); } }

    public void Configure(float size, int depth, LayerMask obstacles)
    {
        rootSize = Mathf.Max(0.1f, size);
        maxLayer = Mathf.Clamp(depth, 0, 6);
        obstacleLayerMask = obstacles;
        octree = null;
    }

    /// <summary>Rebuild after changing static obstacles or the volume position.</summary>
    [ContextMenu("Build Navigation Volume")]
    public void Build()
    {
        rootSize = Mathf.Max(0.1f, rootSize);
        maxLayer = Mathf.Clamp(maxLayer, 0, 6);
        layerCount = 0;
        visitedOctants = new HashSet<int>();
        Physics.SyncTransforms();
        var startTime = System.DateTime.UtcNow;
        CreateOctree();
        MarkTransitableOctants();
        var endTime = System.DateTime.UtcNow;
        var octreeCreationTime = endTime - startTime;

        if (verboseMode)
        {
            Debug.Log("Octree Created Time : " + octreeCreationTime);
            Debug.Log("Octree Created Count: " + octree.Count);
            Debug.Log("Octree Created Layer: " + layerCount);
        }

        startTime = System.DateTime.UtcNow;
        CreateNeighbourLinks();
        endTime = System.DateTime.UtcNow;

        var octreeNeighbourCreation = (endTime - startTime);

        if (verboseMode)
        {
            Debug.Log("Octree Neighbours  : " + octreeNeighbourCreation);
            Debug.Log("Octree Total       : " + (octreeCreationTime + octreeNeighbourCreation));
        }
    }

    private void CreateOctree()
    {
        Octant root = new Octant();
        root.position = transform.position;
        root.size = rootSize;
        root.layer = 0;
        root.parentIdx = -1;

        octree = new List<Octant>();
        octree.Add(root);

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(0);

        int count = 0;
        while (queue.Count > 0)
        {
            int currentOctantIdx = queue.Dequeue();
            Octant currentOctant = octree[currentOctantIdx];

            bool collides = Physics.CheckBox(currentOctant.position, Vector3.one * currentOctant.size * 0.5f, Quaternion.identity, obstacleLayerMask, QueryTriggerInteraction.Ignore);
            if (collides)
            {
                if (currentOctant.layer >= maxLayer) // Check if continue
                {
                    currentOctant.isLeaf = true;
                    currentOctant.isClear = false;
                    currentOctant.isTransitable = false;
                    currentOctant.firstChildIdx = -1;
                }
                else //Subdivide the octant
                {
                    currentOctant.firstChildIdx = octree.Count;

                    float halfSize = currentOctant.size / 2.0f;
                    float quarterSize = halfSize / 2.0f;

                    for (uint pos = 0; pos < 8; pos++)
                    {
                        Vector3 newPosition = new Vector3(
                        currentOctant.position.x + ((pos & (1 << 2)) != 0 ? quarterSize : -quarterSize),
                        currentOctant.position.y + ((pos & (1 << 1)) != 0 ? quarterSize : -quarterSize),
                        currentOctant.position.z + ((pos & (1 << 0)) != 0 ? quarterSize : -quarterSize)
                        );

                        Octant child = new Octant();
                        child.position = newPosition;
                        child.size = halfSize;
                        child.layer = currentOctant.layer + 1;
                        child.parentIdx = currentOctantIdx;

                        queue.Enqueue(octree.Count);

                        octree.Add(child);

                        layerCount = currentOctant.layer + 1;
                    }
                }
            }
            else
            {
                currentOctant.isLeaf = true;
                currentOctant.isClear = true;
                currentOctant.isTransitable = true;
                currentOctant.firstChildIdx = -1;
            }

            octree[currentOctantIdx] = currentOctant;
        }
    }

    private void MarkTransitableOctants()
    {
        for (int i = octree.Count - 1; i > 0; i--)
        {
            Octant octant = octree[i];
            if (octree[octant.parentIdx].isTransitable) { continue; }
            if ((octant.isLeaf && octant.isClear) || octant.isTransitable)
            {
                Octant parent = octree[octant.parentIdx];
                parent.isTransitable = true;
                octree[octant.parentIdx] = parent;
            }
        }
    }

    private void CreateNeighbourLinks()
    {
        for (int i = 0; i < octree.Count; i++)
        {
            Octant octant = octree[i];

            if (octant.layer == 0) { continue; }
            if (!octant.isClear) { continue; }
            if (!octant.isLeaf) { continue; }

            Octant parent = octree[octant.parentIdx];

            int offset = parent.firstChildIdx;
            int position = i - offset;

            for (int axis = 2; axis >= 0; axis--)
            {
                bool inPositiveAxis = (position & (1 << axis)) != 0;
                int neighPos = position ^ (1 << axis);

                // Calculate Same Parent neighbour
                int neighSameParentIdx = offset + neighPos;
                Octant possibleNeighbourSameParent = octree[neighSameParentIdx];
                neighSameParentIdx = possibleNeighbourSameParent.isLeaf && !possibleNeighbourSameParent.isClear ? -1 : offset + neighPos;

                // Calculate Other Parent neighbour
                int neighbourParentIdx = SearchNeighbourIdx(axis, position, i);

                int neighOtherParentIdx = 0;
                if (neighbourParentIdx <= 0) { neighOtherParentIdx = -1; }
                else
                {
                    Octant higherParent = neighbourParentIdx > 0 ? octree[octree[neighbourParentIdx].parentIdx] : octree[0];
                    int parentNeighPos = (neighbourParentIdx - higherParent.firstChildIdx) ^ (1 << axis);

                    neighOtherParentIdx = higherParent.firstChildIdx + parentNeighPos;
                    Octant possibleNeighbourOtherParent = octree[neighOtherParentIdx];
                    neighOtherParentIdx = possibleNeighbourOtherParent.isLeaf && !possibleNeighbourOtherParent.isClear ? -1 : higherParent.firstChildIdx + parentNeighPos;
                }

                switch (axis)
                {
                    case 2: // X Axis
                        octant.neighbourPosX = inPositiveAxis ? neighOtherParentIdx : neighSameParentIdx;
                        octant.neighbourNegX = inPositiveAxis ? neighSameParentIdx : neighOtherParentIdx;
                        break;
                    case 1: // Y Axis
                        octant.neighbourPosY = inPositiveAxis ? neighOtherParentIdx : neighSameParentIdx;
                        octant.neighbourNegY = inPositiveAxis ? neighSameParentIdx : neighOtherParentIdx;
                        break;
                    case 0: // Z Axis
                        octant.neighbourPosZ = inPositiveAxis ? neighOtherParentIdx : neighSameParentIdx;
                        octant.neighbourNegZ = inPositiveAxis ? neighSameParentIdx : neighOtherParentIdx;
                        break;
                    default: Debug.LogError("Error in the axis"); break;
                }
            }

            octree[i] = octant;
        }
    }

    private int SearchNeighbourIdx(int axis, int lastPosition, int currentIdx)
    {
        if (currentIdx == 0) { return -1; }

        int currentPosition = currentIdx - octree[octree[currentIdx].parentIdx].firstChildIdx;
        bool inSamePartAxis = (lastPosition & (1 << axis)) == (currentPosition & (1 << axis));

        if (inSamePartAxis) { return SearchNeighbourIdx(axis, currentPosition, octree[currentIdx].parentIdx); }
        else { return currentIdx; }
    }

    /// <summary>Returns leaf centers; empty means no route. Endpoints are projected to clear cells.</summary>
    public List<Octant> CreatePath(Vector3 a, Vector3 b)
    {
        if (!IsBuilt) { Build(); }
        visitedOctants = new HashSet<int>();
        var path = new List<Octant>();
        if (!octree[0].isTransitable) { return path; }
        int start = GetIdxFromOctant(GetClosestOctant(a));
        int goal = GetIdxFromOctant(GetClosestOctant(b));
        if (start < 0 || goal < 0) { return path; }
        var open = new List<int> { start };
        var costs = new Dictionary<int, float> { { start, 0f } };
        var parents = new Dictionary<int, int>();
        int iterations = 0;
        while (open.Count > 0 && iterations++ < emergencyExit)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
            {
                float candidate = costs[open[i]] + Vector3.Distance(octree[open[i]].position, octree[goal].position);
                float current = costs[open[best]] + Vector3.Distance(octree[open[best]].position, octree[goal].position);
                if (candidate < current) { best = i; }
            }
            int node = open[best];
            open.RemoveAt(best);
            visitedOctants.Add(node);
            if (node == goal)
            {
                while (node != start) { path.Add(octree[node]); node = parents[node]; }
                path.Add(octree[start]);
                path.Reverse();
                return path;
            }
            foreach (int neighbour in GetAllNeighbours(octree[node]))
            {
                if (visitedOctants.Contains(neighbour)) { continue; }
                float score = costs[node] + Vector3.Distance(octree[node].position, octree[neighbour].position);
                if (costs.TryGetValue(neighbour, out float oldScore) && score >= oldScore) { continue; }
                costs[neighbour] = score;
                parents[neighbour] = node;
                if (!open.Contains(neighbour)) { open.Add(neighbour); }
            }
        }
        if (verboseMode) { Debug.LogWarning("Spatial Navigator: no route found or search budget exhausted.", this); }
        return path;
    }

    private List<int> GetAllNeighbours(Octant octant)
    {
        HashSet<int> neighbours = new HashSet<int>();
        foreach (int neighIdx in GetNeighboursIdx(octant))
        {
            for (int i = 0; i < 6; i++)
            {
                if (neighIdx < 0) { continue; }
                foreach (int innerNeighbourIdx in GetAllNeighbours(octant, octree[neighIdx], i)) { neighbours.Add(innerNeighbourIdx); }
            }
        }

        return new List<int>(neighbours);
    }

    private List<int> GetAllNeighbours(Octant octant, Octant neighOctant, int axis)
    {
        if (neighOctant.isClear) { return new List<int> { GetIdxFromOctant(neighOctant) }; }
        if (neighOctant.isLeaf || !neighOctant.isTransitable) { return new List<int>(); }

        List<int> neighbours = new List<int>();
        for (int i = neighOctant.firstChildIdx; i < neighOctant.firstChildIdx + 8; i++)
        {
            Octant child = octree[i];
            if (child.isTransitable && IsNeighbour(octant, child, axis))
            {
                neighbours.AddRange(GetAllNeighbours(octant, octree[i], axis));
            }
        }

        return neighbours;
    }

    // TODO: Save them in a dictionary
    private bool IsNeighbour(Octant a, Octant b, int axis)
    {
        if (axis < 0 || axis > 5) { Debug.LogError("IsNeighbour(): Error with the axis"); return false; }

        float halfA = a.size * 0.5f;
        float halfB = b.size * 0.5f;

        int dimension = axis / 2;
        int direction = axis % 2 == 0 ? 1 : -1;

        // Faces aligned
        float faceA = a.position[dimension] + (halfA * direction);
        float faceB = b.position[dimension] - (halfB * direction);

        bool facesAligned = Mathf.Abs(faceA - faceB) <= epsilon;
        if (!facesAligned) { return false; }

        // Check other faces
        float containmentMargin = Mathf.Abs(halfA - halfB);
        for (int i = 0; i < 3; i++)
        {
            if (dimension == i) { continue; }

            float centerDistance = Mathf.Abs(a.position[i] - b.position[i]);

            if (centerDistance > containmentMargin + epsilon) { return false; }
        }

        return true;
    }

    private int GetIdxFromOctant(Octant octant)
    {
        if (octant.parentIdx < 0) { return 0; }
        Octant parent = octree[octant.parentIdx];

        for (int i = parent.firstChildIdx; i < parent.firstChildIdx + 8; i++)
        {
            if (octree[i].position == octant.position) { return i; }
        }

        return -1;
    }

    private Octant GetClosestOctant(Vector3 position)
    {
        Vector3 halfSize = Vector3.one * (rootSize * 0.5f);
        Vector3 minPosition = transform.position - halfSize;
        Vector3 maxPosition = transform.position + halfSize;

        for (int i = 0; i < 3; i++) { position[i] = Mathf.Clamp(position[i], minPosition[i], maxPosition[i]); } // Fit the position inside the navigation mesh

        return GetClosestOctant(position, octree[0]);
    }

    private Octant GetClosestOctant(Vector3 position, Octant origin)
    {
        if (!OctantContainsPosition(origin, position)) { return origin; }

        Octant currentOctant = origin;

        while (!currentOctant.isClear)
        {
            // Check the children
            int closestChild = -1;
            for (int i = currentOctant.firstChildIdx; i < currentOctant.firstChildIdx + 8; i++)
            {
                Octant child = octree[i];
                if (child.isTransitable)
                {
                    if (OctantContainsPosition(child, position))
                    {
                        currentOctant = child;
                        closestChild = -1; // No need to search on the other children
                        break;
                    }

                    if (closestChild < 0) { closestChild = i; }
                    else { closestChild = Vector3.Distance(child.position, position) < Vector3.Distance(octree[closestChild].position, position) ? i : closestChild; }
                }
            }

            if (closestChild >= 0) { currentOctant = octree[closestChild]; }
        }

        return currentOctant;
    }

    private bool OctantContainsPosition(Octant octant, Vector3 position)
    {
        float halfSize = octant.size / 2.0f;
        return
        ((octant.position.x - halfSize) <= position.x && position.x <= (octant.position.x + halfSize)) &&
        ((octant.position.y - halfSize) <= position.y && position.y <= (octant.position.y + halfSize)) &&
        ((octant.position.z - halfSize) <= position.z && position.z <= (octant.position.z + halfSize));
    }

    private int[] GetNeighboursIdx(Octant octant)
    {
        List<int> neighboursIdxList = new List<int>();

        neighboursIdxList.Add(octant.neighbourPosX);
        neighboursIdxList.Add(octant.neighbourNegX);
        neighboursIdxList.Add(octant.neighbourPosY);
        neighboursIdxList.Add(octant.neighbourNegY);
        neighboursIdxList.Add(octant.neighbourPosZ);
        neighboursIdxList.Add(octant.neighbourNegZ);

        return neighboursIdxList.ToArray();
    }

    private static float ManhattanDistance(Vector3 a, Vector3 b) { return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z); }

    public List<int> GetVisitedOctants()
    {
        List<int> list = new List<int>();
        if (visitedOctants != null) { foreach (int i in visitedOctants) { list.Add(i); } }

        return list;
    }

    /*
    ***************************************************************************************
    * DEBUG
    ***************************************************************************************
    */

    private void OnDrawGizmos()
    {
        if (octree != null)
        {
            if (!debugMode) { return; }

            if (focusedOctant > 0 && focusedOctant < octree.Count)
            {
                DrawOctant(octree[focusedOctant], showNeighbours);
                focusedOctantParent = octree[focusedOctant].parentIdx;
                if (showParent) { DrawOctant(focusedOctantParent, false); }
                if (showAllOctants) { DrawOctree(octree[0], false); }
                if (showNeighbours) { neighbours = GetAllNeighbours(octree[focusedOctant]); }
                if (showChildren)
                {
                    children = new List<int>();
                    if (!octree[focusedOctant].isLeaf)
                    {
                        for (int i = octree[focusedOctant].firstChildIdx; i < octree[focusedOctant].firstChildIdx + 8; i++)
                        {
                            children.Add(i);
                            if (i >= 0) { DrawOctant(i, false); }
                        }
                    }
                }
            }
            else { DrawOctree(octree[0], showNeighbours); }
        }
        else { DrawGrid(); }
    }

    private void DrawOctree(Octant octant, bool drawNeighbours)
    {
        if (octant.isLeaf) { DrawOctant(octant, drawNeighbours); }
        else
        {
            for (int i = octant.firstChildIdx; i < octant.firstChildIdx + 8; i++)
            {
                DrawOctree(octree[i], drawNeighbours);
            }
        }
    }

    public void DrawOctant(int octantIdx, bool drawNeighbours) { DrawOctant(octree[octantIdx], drawNeighbours, true); }
    public void DrawOctant(int octantIdx, bool drawNeighbours, bool highlight) { DrawOctant(octree[octantIdx], drawNeighbours, highlight); }
    public void DrawOctant(Octant octant, bool drawNeighbours) { DrawOctant(octant, drawNeighbours, true); }
    public void DrawOctant(Octant octant, bool drawNeighbours, bool highlight)
    {
        if ((octant.isTransitable && !octant.isClear) && (showFreeOctants || showCollidingOctants))
        {
            Gizmos.color = highlight ? Color.yellow : Color.grey;
            Gizmos.DrawWireCube(octant.position, new Vector3(octant.size, octant.size, octant.size));
        }
        else if (octant.isClear && showFreeOctants)
        {
            Gizmos.color = highlight ? Color.green : Color.grey;
            Gizmos.DrawWireCube(octant.position, new Vector3(octant.size, octant.size, octant.size));
        }
        else if (!octant.isClear && showCollidingOctants)
        {
            Gizmos.color = highlight ? Color.red : Color.grey;
            Gizmos.DrawWireCube(octant.position, new Vector3(octant.size, octant.size, octant.size));
        }

        if (drawNeighbours)
        {
            Gizmos.color = Color.blue;

            // X
            int neighbourIdx = octant.neighbourPosX;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            neighbourIdx = octant.neighbourNegX;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            // Y
            neighbourIdx = octant.neighbourPosY;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            neighbourIdx = octant.neighbourNegY;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            // Z
            neighbourIdx = octant.neighbourPosZ;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            neighbourIdx = octant.neighbourNegZ;
            if (neighbourIdx > 0) { Gizmos.DrawLine(octant.position, octree[neighbourIdx].position); }

            foreach (var n in GetNeighboursIdx(octant))
            {
                for (int i = 0; i < 6; i++)
                {
                    if (n < 0) { continue; }
                    foreach (var a in GetAllNeighbours(octant, octree[n], i))
                    {
                        Octant b = octree[a];
                        Gizmos.color = Color.grey;
                        Gizmos.DrawWireCube(b.position, new Vector3(b.size, b.size, b.size));
                    }
                }
            }
        }
    }

    private void DrawGrid()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(rootSize, rootSize, rootSize));
    }
}
