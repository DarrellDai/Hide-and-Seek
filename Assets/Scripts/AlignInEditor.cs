using UnityEngine;

/* Written by Elmar Hanlhofer  http://www.plop.at  2015 06 10*/

[ExecuteInEditMode]
public class AlignInEditor : MonoBehaviour
{
    public bool align;
    public bool showLineToSurface;
    private RaycastHit hitInfo;
    private bool isHit;
    private readonly float multiplier = 100;
    private Collider objectToPlaceCollider;
    private float offset;
    private Ray ray;

    private void Update()
    {
        if (align)
        {
            transform.position += transform.up * multiplier;
            ray = new Ray(transform.position, -transform.up);
            isHit = Physics.Raycast(ray, out hitInfo, 10000,
                1 << LayerMask.NameToLayer("Terrain"));

            if (isHit)
            {
                objectToPlaceCollider = GetComponent<Collider>();
                offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                    transform.position);
                transform.position = hitInfo.point + offset * hitInfo.normal;
                var forwardVector = transform.forward;
                transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
                var newForwardVector = Vector3.Dot(transform.right, forwardVector) * transform.right +
                                       Vector3.Dot(transform.forward, forwardVector) * transform.forward;
                transform.rotation = Quaternion.LookRotation(newForwardVector, hitInfo.normal);
                Debug.Log(transform.name + " aligned.");
            }
            else
            {
                Debug.Log("No surface found for " + transform.name);
            }

            align = false;
        }

        if (showLineToSurface)
        {
            RaycastHit hit;
            var ray = new Ray(transform.position + transform.up * multiplier, -transform.up);
            if (Physics.Raycast(ray, out hit, 10000,
                    1 << LayerMask.NameToLayer("Terrain")))
                Debug.DrawLine(transform.position, hit.point);
        }
    }
}