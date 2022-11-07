using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    //Radius of field of view
    public float viewRadius;

    public LayerMask targetMask;

    public LayerMask obstacleMask;

    //If any hider detected 
    [HideInInspector] public bool isDetected;

    //Point on each angle and height for field of view
    [HideInInspector] public List<Vector3> viewPoints;

    //The origin of the raycast
    private Vector3 rayOrigin;

    //Total number of x direction steps
    public int xDirectionResolution;

    //Total number of y direction steps
    public int yDirectionResolution;

    private GameObject fieldOfViewSpawner;

    //Difference bewteen two point at x direction on the plane perpendicular to the vector from origin to target
    private float xDirectionstepSize;

    //Difference bewteen two point at y direction on the plane perpendicular to the vector from origin to target
    private float yDirectionstepSize;
    


    /// <summary>
    ///     Get viewPoints on each angle and height. Set isDetected = true if a hider is detected. Calculate viewPoints on all
    ///     angles and heights if drawFieldOfView = true, or return once a hider is detected.
    /// </summary>
    public void CalculateFieldOfView(Transform detectedTransform)
    {
        isDetected = false;
        rayOrigin = transform.Find("Eye").position;
        viewPoints = new List<Vector3>();
        var detectedRendererPosition = detectedTransform.GetComponent<Renderer>().bounds.center;
        var detectedRendererSize = detectedTransform.GetComponent<Renderer>().bounds.size.magnitude;
        var directionFromOriginToTarget = (detectedRendererPosition - rayOrigin).normalized;
        var xDirection = new Vector3(
            Mathf.Sqrt(Mathf.Pow(directionFromOriginToTarget.x, 2) /
                       (Mathf.Pow(directionFromOriginToTarget.x, 2) + Mathf.Pow(directionFromOriginToTarget.z, 2))), 0,
            Mathf.Sqrt(Mathf.Pow(directionFromOriginToTarget.z, 2) /
                       (Mathf.Pow(directionFromOriginToTarget.z, 2) + Mathf.Pow(directionFromOriginToTarget.z, 2))));
        var yDirection = Vector3.Cross(xDirection, directionFromOriginToTarget);
        xDirectionstepSize = detectedRendererSize / xDirectionResolution;
        yDirectionstepSize = detectedRendererSize / yDirectionResolution;
        var oldViewCast = new ViewCastInfo();
        for (var i = 0; i <= xDirectionResolution; i++)
        {
            for (var j = 0; j <= yDirectionResolution; j++)
            {
                var targetPoint = detectedRendererPosition - (xDirection + yDirection) * detectedRendererSize / 2 +
                                  i * xDirectionstepSize * xDirection + j * yDirectionstepSize * yDirection;
                var newViewCast = ViewCast(targetPoint);
                
                if (i > 0)
                    if (oldViewCast.tag == "Hider")
                    {
                        isDetected = true;
                    }

                viewPoints.Add(newViewCast.point);
                oldViewCast = newViewCast;
            }
        }
    }
    

    /// <summary>
    ///     Construct ViewCastInfo from angle and height.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private ViewCastInfo ViewCast(Vector3 targetPoint)
    {
        var dir = (targetPoint - rayOrigin).normalized;
        RaycastHit hit;
        RaycastHit hitTarget;

        if (Physics.Raycast(rayOrigin, dir, out hit, viewRadius, obstacleMask))
        {
            if (Physics.Raycast(rayOrigin, dir, out hitTarget, hit.distance,
                    targetMask))
                return new ViewCastInfo(true, hit.point, hitTarget.point, hit.distance, hitTarget.transform.tag,
                    hitTarget.transform.Find("Body").GetComponent<Renderer>());
            return new ViewCastInfo(true, hit.point, hit.point, hit.distance, null, null);
        }

        if (Physics.Raycast(rayOrigin, dir, out hitTarget, viewRadius, targetMask))
            return new ViewCastInfo(false, rayOrigin + dir * viewRadius,
                hitTarget.point, viewRadius, hitTarget.transform.tag,
                hitTarget.transform.Find("Body").GetComponent<Renderer>());
        return new ViewCastInfo(false, rayOrigin + dir * viewRadius,
            rayOrigin + dir * viewRadius, viewRadius, null, null);
    }


    /// <summary>
    ///     Containing if a raycast hits an obstacle, the hitPoint of an obstacle, the hitPoint of a target and its tag, and
    ///     its distance and angle.
    /// </summary>
    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public Vector3 targetPoint;
        public float dst;
        public string tag;
        public Renderer detectedRenderer;

        public ViewCastInfo(bool _hit, Vector3 _point, Vector3 _targetPoint, float _dst, string _tag,
            Renderer _detectedRenderer)
        {
            hit = _hit;
            point = _point;
            targetPoint = _targetPoint;
            dst = _dst;
            tag = _tag;
            detectedRenderer = _detectedRenderer;
        }
    }
}