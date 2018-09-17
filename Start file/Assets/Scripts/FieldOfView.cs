using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//target & obstacle layer
//script on controlled char
public class FieldOfView : MonoBehaviour {

    public float viewRadius;
    [Range(0,360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    //Maintain a list of all Targets visible
    public List<Transform> visibleTargets = new List<Transform>();

    //put on child with meshfilter & mesh renderer component, reset child to parent pos
    public float meshResolution;
    public int edgeResolveIterations;
    //if two obstacles get hit only render the first one
    public float edgeDistanceThreshold;
    public MeshFilter viewMeshFilter;
    Mesh ViewMesh;

    private void Start()
    {
        ViewMesh = new Mesh();
        ViewMesh.name = "View Mesh";
        viewMeshFilter.mesh = ViewMesh;

        StartCoroutine("FindTargetsWithDelay", .2f);
    }

    //only update after object finished rotating or it jitters
    private void LateUpdate()
    {
        DrawFieldOfView();
    }

    //only search for visible targets with delayed update
    IEnumerator FindTargetsWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTargets();
        }
    }

    void FindVisibleTargets()
    {
        //each time this method will called we clear the list of visible targets to dont get dublicates
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for(int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            //check if the target falls within view angle, find the direction between char and target and use the dirFromAngle method to find the angle
            Vector3 dirToTarget = (target.position - transform.position).normalized;
            if(Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                //look if there is an obstacle between
                float distToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    visibleTargets.Add(target);
                }
            }
        }
    }

    void DrawFieldOfView()
    {
        //number of rays to cast per degree
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        //how many degrees in each steps
        float stepAngles = viewAngle / stepCount;
        //List of all the points the viewcast hits to construct mesh
        List<Vector3> viewPoints = new List<Vector3>();
        //Edge: if previews viewcast hit obstacle
        ViewCastInfo oldViewCast = new ViewCastInfo();
        for(int i = 0; i <= stepCount; i++)
        {
            //rotate curront rotation back to the left most angle, then rotate stepwise clockwise to right moust viewangle
            float angle = transform.eulerAngles.y - viewAngle / 2 + stepAngles * i;
            ViewCastInfo newViewCast = ViewCast(angle);

            //first iteration the oldviewcast is not set            
            if (i > 0)
            {
                //value between two hits
                bool edgeDstThresholdExceeded = Mathf.Abs(oldViewCast.dst - newViewCast.dst) > edgeDistanceThreshold;
                //oldviewcast and the new one didnt or the old one didnt and the new one did
                if(oldViewCast.hit != newViewCast.hit || (oldViewCast.hit && newViewCast.hit && edgeDstThresholdExceeded))
                {
                    //find the edge
                    EdgeInfo edge = FindEdge(oldViewCast, newViewCast);
                    //if the dont have the default value
                    if(edge.pointA != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointA);
                    }
                    if (edge.pointB != Vector3.zero)
                    {
                        viewPoints.Add(edge.pointB);
                    }
                }
            }
            viewPoints.Add(newViewCast.point);
            oldViewCast = newViewCast;
            Debug.DrawLine(transform.position, transform.position + DirFromAngle(angle, true) * viewRadius, Color.green);
        }

        //length of array is vertices - 2 * 3, vertices for all viewpoints + transform and 3 to generate a triangle from each corner
        int vertexCount = viewPoints.Count + 1;
        //unity generates of each 3 pair position in the array a trianglemesh
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];

        //viewmesh is child of character obj, so all positions of vertices need to be localspace(relative to character)
        //first vertex = transform position = localspace = vector3.zero
        vertices[0] = Vector3.zero;
        for(int i= 0; i < vertexCount-1; i++)
        {
            //so it wont go out of bounds
            if(i < vertexCount - 2) { 
            //always go back to sourcepoint, convert viewpoints to localspace
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);
            //triangle array, first vertex of each triangle
            triangles[i * 3] = 0;
            //next vertex of the triangle
            triangles[i * 3 + 1] = i + 1;
            //last vertex of the triangle
            triangles[i * 3 + 2] = i + 2;
            }
        }

        ViewMesh.Clear();
        ViewMesh.vertices = vertices;
        ViewMesh.triangles = triangles;
        ViewMesh.RecalculateNormals();
    }

    EdgeInfo FindEdge(ViewCastInfo minViewCast, ViewCastInfo maxViewCast)
    {
        float minAngle = minViewCast.angle;
        float maxAngle = maxViewCast.angle;
        Vector3 minPoint = Vector3.zero;
        Vector3 maxPoint = Vector3.zero;

        //each step cast a ray between min and max angle
        for (int i = 0; i < edgeResolveIterations; i++)
        {
            //find new angle
            float angle = (minAngle + maxAngle) / 2;
            ViewCastInfo newViewCast = ViewCast(angle);

            bool edgeDstThresholdExceeded = Mathf.Abs(minViewCast.dst - newViewCast.dst) > edgeDistanceThreshold;
            if (newViewCast.hit == minViewCast.hit && !edgeDstThresholdExceeded)
            {
                minAngle = angle;
                minPoint = newViewCast.point;
            }
            else
            {
                maxAngle = angle;
                maxPoint = newViewCast.point;
            }
        }

        return new EdgeInfo(minPoint, maxPoint);
    }

    //method to handle raycast 
    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);
        RaycastHit hit;

        //if hit something only draw to his corner
        if (Physics.Raycast(transform.position, dir, out hit, viewRadius, obstacleMask))
        {
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        }
        //if we did not hit something draw full line
        else
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
    }

    //take an angle and spit out the direction of that angle
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        //if angle is not global, convert to local angle by add the transform own rotation, rotate with object
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        //trigonometrie circle, unity starts with 0 on top and goes counter clockwise
        //convert unity circle to trig circle, 90-x = swap sin with cos
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }


        //bunch of information about the raycast
    public struct ViewCastInfo
    {
        public bool hit;
        //endpoint of the ray
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool hit, Vector3 point, float dst, float angle)
        {
            this.hit = hit;
            this.point = point;
            this.dst = dst;
            this.angle = angle;
        }
    }

    //resolution for jittering at edges, shoots rays between the last and the next to last unitl it finds the edge
    public struct EdgeInfo
    {
        public Vector3 pointA;
        public Vector3 pointB;

        public EdgeInfo(Vector3 pointA, Vector3 pointB)
        {
            this.pointA = pointA;
            this.pointB = pointB;
        }
    }
}
