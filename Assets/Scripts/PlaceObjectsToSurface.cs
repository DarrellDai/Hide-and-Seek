using System;
using UnityEngine;

public class PlaceObjectsToSurface : MonoBehaviour
{
    private RaycastHit hitInfo;
    private RaycastHit hitInfoDown;
    private RaycastHit hitInfoNormal;
    private bool isHit;
    private bool isHitDown;
    private bool isHitNormal;
    private readonly float multiplier = 10;
    private Collider objectToPlaceCollider;
    private Vector3 offset;
    private Ray ray;
    private Ray rayDown;
    private Ray rayNormal;

    /// <summary>
    ///     Use raycast to place game object to a surface, so that it's normal to the surface and forward direction is adjusted
    ///     with minimal change.
    /// </summary>
    public void StartPlacing()
    {

        //Lift the object along its normal direction so that it's above the surface 
        transform.position += transform.up * multiplier;
        Physics.SyncTransforms();
        //Cast downward ray along its normal direction
        ray = new Ray(transform.position, -transform.up);
        isHit = Physics.Raycast(ray, out hitInfo, 1000,
            1 << LayerMask.NameToLayer("Terrain"));
        if (isHit)
        {
            
            objectToPlaceCollider = GetComponent<Collider>();
            Debug.DrawLine(objectToPlaceCollider.ClosestPoint(hitInfo.point), objectToPlaceCollider.bounds.center);
            //Calculate the distance from bottom to center in the object
            offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                         objectToPlaceCollider.bounds.center) * hitInfo.normal + transform.position -
                     objectToPlaceCollider.bounds.center;
            
            /*offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                objectToPlaceCollider.bounds.center) * hitInfo.normal;*/
            //offset = objectToPlaceCollider.bounds.extents.magnitude / 2 * hitInfo.normal;
            //Save old forward direction
            var forwardVector = transform.forward;
            //Change the destinationPosition so the bottom of the object touches the surface
            
            //Rotate the object to normal to the surface
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            //Project the old forward direction to new right and forward plane so the change is minimum
            var newForwardVector = Vector3.Dot(transform.right, forwardVector) * transform.right +
                                   Vector3.Dot(transform.forward, forwardVector) * transform.forward;
            //Rotate the object to new forward direction
            transform.rotation = Quaternion.LookRotation(newForwardVector, hitInfo.normal);
            transform.position = hitInfo.point + offset;
        }
    }
}