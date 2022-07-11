using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class FieldOfView : MonoBehaviour
{
    //Radius of field of view
    public float viewRadius;

    [Range(0, 360)]
    //Angle of field of view
    public float viewAngle;

    //Height of field of view
    public float ViewHeight;

    //Number of rays of unit angle
    public float angleResolution;

    //Number of rays of unit height
    public float heightResolution;

    //Total number of angle steps
    int angleStepCount;

    //Total number of height steps
    int heightStepCount;

    //Difference bewteen two angle steps
    float stepAngleSize;

    //Difference bewteen two height steps
    float stepHeightSize;


    public LayerMask targetMask;

    public LayerMask obstacleMask;

    //Position of as detected hider
    [HideInInspector] public Vector3 detectPosition;

    //If any hider detected 
    [HideInInspector] public bool isDetected;

    //If draw field of view
    public bool drawFieldOfView = false;

    //Point on each angle and height for field of view
    [HideInInspector] public List<Vector3> viewPoints;
    [HideInInspector] public MeshFilter viewMeshFilter;
    [HideInInspector] public Mesh viewMesh;

    //Field of View object to instantiate
    public GameObject FOV;
    
    private GameObject fieldOfViewSpawner;

    public void InitializeMesh()
    {
        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        fieldOfViewSpawner = FindObjectOfType<PlayerSpawner>().fieldOfViewSpawner;
        GameObject FOVMesh = Instantiate(FOV);
        FOVMesh.transform.parent = fieldOfViewSpawner.transform;
        viewMeshFilter = FOVMesh.GetComponent<MeshFilter>();
        viewMeshFilter.mesh = viewMesh;
        viewMeshFilter.transform.position = Vector3.zero;
        viewMeshFilter.transform.rotation = quaternion.identity;
    }

    public void InitializeParameters()
    {
        angleStepCount = Mathf.RoundToInt(viewAngle * angleResolution);
        heightStepCount = Mathf.RoundToInt(ViewHeight * heightResolution);
        stepAngleSize = viewAngle / angleStepCount;
        stepHeightSize = ViewHeight / heightStepCount;
        isDetected = false;
    }

    /// <summary>
    /// Get viewPoints on each angle and height. Set isDetected = true if a hider is detected. Calculate viewPoints on all
    /// angles and heights if drawFieldOfView = true, or return once a hider is detected.
    /// </summary>
    public void CalculateFieldOfView()
    {
        viewPoints = new List<Vector3>();

        for (int j = 0; j <= heightStepCount; j++)
        {
            float height = -ViewHeight / 2 + stepHeightSize * j;
            ViewCastInfo oldViewCast = new ViewCastInfo();
            for (int i = 0; i <= angleStepCount; i++)
            {
                float angle = -viewAngle / 2 + stepAngleSize * i;
                ViewCastInfo newViewCast = ViewCast(angle, height);

                if (i > 0)
                {
                    if (oldViewCast.tag == "Hider")
                    {
                        detectPosition = oldViewCast.targetPoint;
                        isDetected = true;
                        if (!drawFieldOfView)
                            return;
                    }
                }

                viewPoints.Add(newViewCast.point);
                oldViewCast = newViewCast;
            }
        }
    }

    /// <summary>
    /// Draw the field of view mesh.
    /// </summary>
    public void DrawFieldOfView()
    {
        int vertexCount = (viewPoints.Count / (heightStepCount + 1) + 1) * (heightStepCount + 1);
        Vector3[] vertices = new Vector3[vertexCount];

        int[] triangles = new int[(viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                                  (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount +
                                  2 * heightStepCount * 2 * 3];
        //Get Vertices
        for (int j = 0; j <= heightStepCount; j++)
        {
            float height = -ViewHeight / 2 + stepHeightSize * j;
            vertices[j * (viewPoints.Count / (heightStepCount + 1) + 1)] = transform.position + transform.up * height;
            for (int i = 1; i < viewPoints.Count / (heightStepCount + 1) + 1; i++)
            {
                vertices[j * (viewPoints.Count / (heightStepCount + 1) + 1) + i] =
                    viewPoints[j * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) + i - 1];
            }
        }
        //Get Triangles
        
        //Top and bottom
        for (int i = 0; i < viewPoints.Count / (heightStepCount + 1) + 1 - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
            triangles[(viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 + i * 3] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * heightStepCount;
            triangles[(viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 + i * 3 + 1] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * heightStepCount + i + 1;
            triangles[(viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 + i * 3 + 2] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * heightStepCount + i + 2;
        }
        
        //Far end
        for (int j = 1; j <= heightStepCount; j++)
        {
            for (int i = 0; i < viewPoints.Count / (heightStepCount + 1) + 1 - 2; i++)
            {
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6] =
                    (j - 1) * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 1;
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6 +
                        1] =
                    j * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 2;
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6 +
                        2] =
                    j * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 1;
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6 +
                        3] =
                    (j - 1) * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 1;
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6 +
                        4] =
                    (j - 1) * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 2;
                triangles[
                        (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                        (j - 1) * ((viewPoints.Count / (heightStepCount + 1) + 1 - 1) * 2 - 2) * 3 + i * 6 +
                        5] =
                    j * (viewPoints.Count / (heightStepCount + 1) + 1) + i + 2;
            }
        }
        //Sides
        for (int j = 0; j <= heightStepCount - 1; j++)
        {
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1);
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 1] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 2] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j + 1;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 3] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1);
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 4] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j + 1;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 5] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1) + 1;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 6] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1);
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 7] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j + (viewPoints.Count / (heightStepCount + 1) + 1) - 1;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 8] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 9] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1);
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 10] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * (j + 1) +
                (viewPoints.Count / (heightStepCount + 1) + 1) -
                1;
            triangles[
                    (viewPoints.Count / (heightStepCount + 1) + 1 - 2) * 3 * 2 +
                    (2 * (viewPoints.Count / (heightStepCount + 1) + 1 - 1) - 2) * 3 * heightStepCount + j * 12 + 11] =
                (viewPoints.Count / (heightStepCount + 1) + 1) * j + (viewPoints.Count / (heightStepCount + 1) + 1) - 1;
        }

        viewMesh.Clear();

        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }
    /// <summary>
    /// Construct ViewCastInfo from angle and height.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    ViewCastInfo ViewCast(float angle, float height)
    {
        Vector3 dir = DirFromAngle(angle);
        RaycastHit hit;
        RaycastHit hitTarget;

        if (Physics.Raycast(transform.position + transform.up * height, dir, out hit, viewRadius, obstacleMask))
        {
            if (Physics.Raycast(transform.position + transform.up * height, dir, out hitTarget, hit.distance,
                    targetMask))
                return new ViewCastInfo(true, hit.point, hitTarget.point, hit.distance, hitTarget.transform.tag, angle);
            return new ViewCastInfo(true, hit.point, hit.point, hit.distance, null, angle);
        }
        else
        {
            if (Physics.Raycast(transform.position + transform.up * height, dir, out hitTarget, viewRadius, targetMask))
                return new ViewCastInfo(false, transform.position + transform.up * height + dir * viewRadius,
                    hitTarget.point, viewRadius, hitTarget.transform.tag, angle);
            return new ViewCastInfo(false, transform.position + transform.up * height + dir * viewRadius,
                transform.position + transform.up * height + dir * viewRadius, viewRadius,
                null, angle);
        }
    }
    /// <summary>
    /// Convert from angle to direction
    /// </summary>
    /// <param name="angleInDegrees"></param>
    /// <returns></returns>
    public Vector3 DirFromAngle(float angleInDegrees)
    {
        Vector3 direction = Quaternion.AngleAxis(angleInDegrees, transform.up) * transform.forward;
        return direction;
    }
    /// <summary>
    /// Containing if a raycast hits an obstacle, the hitPoint of an obstacle, the hitPoint of a target and its tag, and its distance and angle. 
    /// </summary>
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public Vector3 targetPoint;
        public float dst;
        public float angle;
        public string tag;

        public ViewCastInfo(bool _hit, Vector3 _point, Vector3 _targetPoint, float _dst, string _tag, float _angle)
        {
            hit = _hit;
            point = _point;
            targetPoint = _targetPoint;
            dst = _dst;
            angle = _angle;
            tag = _tag;
        }
    }
    
}