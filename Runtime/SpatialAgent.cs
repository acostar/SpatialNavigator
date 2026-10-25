using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SphereCheck
{
    public Vector3 start;
    public Vector3 end;

    public bool blocked;
}

public class SpatialAgent : MonoBehaviour
{
    [Header("Basic Configuration")]
    [SerializeField] private SpatialNavigationMesh navigationMesh;
    [SerializeField] private bool simplifyPath;
    [SerializeField] private bool moveDirectly;
    [SerializeField] private Vector3 center;
    [SerializeField, Min(0.001f)] private float navigationRadius = 1.0f;
    [SerializeField, Min(0.001f)] private float approachHeight = 0f;
    [SerializeField] private LayerMask collisionLayers = Physics.DefaultRaycastLayers;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float minValidDistance = 0.2f;
    [SerializeField] private float recalculatePathTime = 0.5f;
    [SerializeField] private float recalculatePathDistance = 1f;
    private float lastPathTime;
    private Vector3 lastTargetPosition;

    [Header("-------------------------------- Debug --------------------------------\n")]
    [SerializeField] private bool debugMode;
    [SerializeField] private bool verboseMode;

    [Header("Config")]
    [SerializeField] private bool showOriginOctant;
    [SerializeField] private bool showTargetOctant;
    [SerializeField] private bool calculatePath;

    [Header("Visited Octants")]
    [SerializeField] private bool showVisitedOctants;
    [SerializeField] private bool animateShowVisitedOctants;
    [SerializeField] private float octantPerSecond;
    private float timeLeft;
    private int animationIdx;

    [Header("Path")]
    [SerializeField] private bool showPath;

    private Vector3 targetPosition;

    private List<Octant> currentPath;
    private List<int> visitedOctants;

    private int currentOctantIdx;

    private SphereCheck lastSphereCheck;

    private List<SphereCheck> sphereChecks;
    private bool recordSphereChecks;

    private void Start()
    {
        currentOctantIdx = 0;
        lastPathTime = recalculatePathTime;

        sphereChecks = new List<SphereCheck>();
    }

    private void OnEnable()
    {
        if (navigationMesh == null) { navigationMesh = FindObjectOfType<SpatialNavigationMesh>(); }
        sphereChecks = new List<SphereCheck>();
    }

    /// <summary>Assign a volume explicitly when the scene contains multiple volumes.</summary>
    public void Configure(SpatialNavigationMesh volume, LayerMask obstacles, float radius = 0.25f)
    {
        navigationMesh = volume;
        collisionLayers = obstacles;
        navigationRadius = Mathf.Max(0.001f, radius);
        currentPath = null;
        lastPathTime = 0f;
    }

    private void Update()
    {
        if (calculatePath) { currentPath = null; if (target != null) { Move(target.position, 0.0f); } calculatePath = false; }

        lastPathTime -= Time.deltaTime;

        // Debug
        if (visitedOctants == null) { return; }
        if (visitedOctants.Count <= 0) { return; }
        if (animateShowVisitedOctants)
        {
            if (timeLeft <= 0)
            {
                if (animationIdx < visitedOctants.Count) { animationIdx++; }
                timeLeft = octantPerSecond;
            }
            else { timeLeft -= Time.deltaTime; }
        }
    }

    public void Move(Vector3 position, float speed)
    {
        if (navigationMesh == null) { return; }
        speed = Mathf.Max(0f, speed);
        targetPosition = position;

        if (moveDirectly && CanMoveDirectly(transform.position, position, false)) { transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime); return; }

        if (currentPath != null && currentPath.Count > 0)
        {
            if (currentOctantIdx == currentPath.Count - 1 && CanMoveDirectly(transform.position, position, false))
            {
                transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
            }
            else if (Vector3.Distance(transform.position, currentPath[currentOctantIdx].position) < minValidDistance) // Reached the current destination
            {
                if (currentOctantIdx + 1 < currentPath.Count) { currentOctantIdx++; }
                else if (CanMoveDirectly(transform.position, position, false))
                {
                    transform.position = Vector3.MoveTowards(transform.position, position, speed * Time.deltaTime);
                }
            }
            else if (CanMoveDirectly(transform.position, currentPath[currentOctantIdx].position, false))
            { transform.position = Vector3.MoveTowards(transform.position, currentPath[currentOctantIdx].position, speed * Time.deltaTime); }
            else { currentPath = null; }
        }

        // Check if it has to recalculate
        if (lastPathTime <= 0 || Vector3.Distance(position, lastTargetPosition) >= recalculatePathDistance || (currentPath == null && lastPathTime <= 0))
        {
            var startTime = System.DateTime.UtcNow;

            List<Octant> newPath = navigationMesh.CreatePath(transform.position, position);
            if (newPath.Count <= 0)
            {
                currentPath = null;
                lastTargetPosition = position;
                lastPathTime = recalculatePathTime;
                if (showVisitedOctants)
                {
                    visitedOctants = navigationMesh.GetVisitedOctants();
                    Debug.Log("Visited Octants_: " + visitedOctants.Count);
                    animationIdx = 0;
                }

                return;
            }

            // Simplify the Path
            if (simplifyPath && newPath.Count > 2)
            {
                List<Octant> simplifiedPath = new List<Octant>();
                for (int i = 0; i < newPath.Count; i++)
                {
                    Octant octant = newPath[i];
                    simplifiedPath.Add(octant);

                    for (int j = newPath.Count - 1; j > i; j--)
                    {
                        if (!CanMoveDirectly(octant.position, newPath[j].position)) { continue; }

                        CanMoveDirectly(octant.position, newPath[j].position);
                        i = j - 1;
                        break;
                    }
                }

                int closestIdx = 0;
                if (currentPath != null && currentPath.Count > 0)
                {
                    if (Vector3.Distance(currentPath[0].position, simplifiedPath[0].position) <= minValidDistance) { closestIdx = simplifiedPath.Count > 1 ? 1 : 0; }
                }

                currentPath = simplifiedPath;
                currentOctantIdx = closestIdx;
            }
            else
            {
                int closestIdx = 0;
                if (currentPath != null && currentPath.Count > 0)
                {
                    if (Vector3.Distance(currentPath[0].position, newPath[0].position) <= minValidDistance) { closestIdx = newPath.Count > 1 ? 1 : 0; }
                }

                currentPath = newPath;
                currentOctantIdx = closestIdx;
            }

            lastTargetPosition = targetPosition;
            lastPathTime = recalculatePathTime;

            if (verboseMode)
            {
                Debug.Log(">>>>>>>> Time Duration<<<<<<<<");
                var endTime = System.DateTime.UtcNow;
                var routeTime = endTime - startTime;
                Debug.Log("SpatialAgent - Find Route: " + routeTime);
            }

            if (showVisitedOctants)
            {
                visitedOctants = navigationMesh.GetVisitedOctants();
                animationIdx = 0;

                if (verboseMode)
                {
                    Debug.Log(">>>>>>>> Visited Octants <<<<<<<<");
                    foreach (var o in visitedOctants) { Debug.Log(">> Visited Octant: " + o); }
                }
            }
        }
    }


    public bool ClearPath(Vector3 a, Vector3 b) { return CanMoveDirectly(a, b, false); }
    public bool ClearPath(Vector3 a, Vector3 b, bool saveCheck) { return CanMoveDirectly(a, b, saveCheck); }
    public bool CanMoveDirectly(Vector3 position) { return CanMoveDirectly(transform.position, position, false); }
    private bool CanMoveDirectly(Vector3 originalPosition, Vector3 nextPosition) { return CanMoveDirectly(originalPosition, nextPosition, false); }
    private bool CanMoveDirectly(Vector3 originalPosition, Vector3 nextPosition, bool saveSphereCheck)
    {
        // It is inside an object
        bool isInsideObject = Physics.CheckSphere(originalPosition, navigationRadius, collisionLayers, QueryTriggerInteraction.Ignore);

        if (isInsideObject)
        {
            if (saveSphereCheck)
            {
                SphereCheck sphereCheck = new SphereCheck();
                sphereCheck.start = originalPosition;
                sphereCheck.end = originalPosition;
                sphereCheck.blocked = true;

                lastSphereCheck = sphereCheck;
                if (recordSphereChecks) { sphereChecks.Add(sphereCheck); }
            }

            return false;
        }

        // Check the displacement
        // Cast along the exact segment used by movement.
        Vector3 destination = nextPosition;
        Vector3 displacement = destination - originalPosition;
        float distance = displacement.magnitude;
        if (distance <= 0.00001f) { return true; }
        Vector3 direction = displacement / distance;

        bool collides = Physics.SphereCast(originalPosition, navigationRadius, direction, out RaycastHit hit, distance, collisionLayers, QueryTriggerInteraction.Ignore);

        if (saveSphereCheck)
        {
            SphereCheck sphereCheck = new SphereCheck();
            sphereCheck.start = originalPosition;
            sphereCheck.end = destination;
            sphereCheck.blocked = collides;

            lastSphereCheck = sphereCheck;
            if (recordSphereChecks) { sphereChecks.Add(sphereCheck); }
        }

        return !collides;
    }

    /*
    ***************************************************************************************
    * Gizmos
    ***************************************************************************************
    */

    public void RecordSphereChecks() { sphereChecks = new List<SphereCheck>(); recordSphereChecks = true; }
    public void StopSphereChecks() { recordSphereChecks = false; }

    private void OnDrawGizmosSelected()
    {
        // Draw Sphere Collider
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, navigationRadius);

        if (lastSphereCheck.blocked) { Gizmos.color = Color.red; }
        else { Gizmos.color = Color.green; }
        DrawSphereSweep(lastSphereCheck.start, lastSphereCheck.end, navigationRadius);

        if (sphereChecks != null && sphereChecks.Count > 0) { foreach (var a in sphereChecks) { DrawSphereSweep(a.start, a.end, navigationRadius); } }
    }

    private void OnDrawGizmos()
    {
        if (debugMode && navigationMesh != null)
        {
            if (currentPath != null && currentPath.Count > 0)
            {
                if (showOriginOctant) { navigationMesh.DrawOctant(currentPath[0], false); }
                if (showTargetOctant && currentPath.Count > 1) { navigationMesh.DrawOctant(currentPath[currentPath.Count - 1], false); }
            }

            if (visitedOctants != null)
            {
                if (showVisitedOctants && visitedOctants.Count > 0)
                {
                    int lastOctant = animateShowVisitedOctants ? animationIdx : visitedOctants.Count;

                    for (int i = 0; i < lastOctant; i++)
                    {
                        int octantIdx = visitedOctants[i];
                        navigationMesh.DrawOctant(octantIdx, false, (i + 1 == lastOctant));
                    }
                }
            }

            if (currentPath != null && showPath)
            {
                for (int i = 1; i < currentPath.Count; i++)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(currentPath[i - 1].position, currentPath[i].position);
                }
            }
        }
    }

    public void DrawSphereSweep(Vector3 start, Vector3 end, float radius)
    {
        Gizmos.DrawWireSphere(start, radius);
        Gizmos.DrawWireSphere(end, radius);
        Gizmos.DrawLine(start, end);

        Vector3 forward = (end - start).normalized;

        Vector3 reference = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;

        Vector3 right = Vector3.Cross(forward, reference).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;

        Gizmos.DrawLine(start + right * radius, end + right * radius);
        Gizmos.DrawLine(start - right * radius, end - right * radius);
        Gizmos.DrawLine(start + up * radius, end + up * radius);
        Gizmos.DrawLine(start - up * radius, end - up * radius);
    }
}
