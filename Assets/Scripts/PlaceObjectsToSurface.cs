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
    // A speed for slerp to make move smooth
    private float smoothSpeed=3f;
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
            //Calculate the distance from bottom to center in the object
            /*offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                         objectToPlaceCollider.bounds.center) * hitInfo.normal + transform.position -
                     objectToPlaceCollider.bounds.center;*/
            //Change the destinationPosition so the bottom of the object touches the surface
            offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                objectToPlaceCollider.bounds.center) * hitInfo.normal;
            transform.position = hitInfo.point + offset;
            //offset = objectToPlaceCollider.bounds.extents.magnitude / 2 * hitInfo.normal;
            //Save old forward direction
            var forwardVector = transform.forward;
            
            
            //Rotate the object to normal to the surface
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            //Project the old forward direction to new right and forward plane so the change is minimum
            var newForwardVector = Vector3.Dot(transform.right, forwardVector) * transform.right +
                                   Vector3.Dot(transform.forward, forwardVector) * transform.forward;
            //Rotate the object to new forward direction
            transform.rotation = Quaternion.LookRotation(newForwardVector, hitInfo.normal);
            
        }
    }

    public void StartCorrecting(float MoveSpeed, Vector3 dirToGo)
    {
        Vector3 originalPosition = transform.position;
        Quaternion originalRotation = transform.rotation;
        //Lift the object along its normal direction so that it's above the surface 
        transform.position += transform.up * multiplier;
        Physics.SyncTransforms();
        //Cast downward ray along its normal direction
        ray = new Ray(transform.position+dirToGo*Time.deltaTime*MoveSpeed, -transform.up);
        isHit = Physics.Raycast(ray, out hitInfo, 1000,
            1 << LayerMask.NameToLayer("Terrain"));
        if (isHit)
        {

            objectToPlaceCollider = GetComponent<Collider>();
            //Calculate the distance from bottom to center in the object
            /*offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                         objectToPlaceCollider.bounds.center) * hitInfo.normal + transform.position -
                     objectToPlaceCollider.bounds.center;*/
            
            //Change the destinationPosition so the bottom of the object touches the surface
            offset = Vector3.Distance(objectToPlaceCollider.ClosestPoint(hitInfo.point),
                objectToPlaceCollider.bounds.center) * hitInfo.normal;
            Vector3 finalPosition = hitInfo.point + offset;
            
            //offset = objectToPlaceCollider.bounds.extents.magnitude / 2 * hitInfo.normal;
            //Save old forward direction
            var forwardVector = transform.forward;
           
            transform.position = Vector3.Lerp(originalPosition, finalPosition, MoveSpeed * Time.deltaTime);
            //Rotate the object to normal to the surface
            transform.rotation = Quaternion.FromToRotation(Vector3.up, hitInfo.normal);
            //Project the old forward direction to new right and forward plane so the change is minimum
            var newForwardVector = Vector3.Dot(transform.right, forwardVector) * transform.right +
                                   Vector3.Dot(transform.forward, forwardVector) * transform.forward;
            //Rotate the object to new forward direction
            transform.rotation = Quaternion.LookRotation(newForwardVector, hitInfo.normal);
            // Prevent sudden change to make the rotation smooth
            transform.rotation = Quaternion.Slerp(originalRotation, transform.rotation, smoothSpeed*Time.deltaTime);
        }
    }
}