using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlaceObject : MonoBehaviour
{
    public GameObject prefabToPlace;

    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        Debug.Log("Linksklick erkannt.");

        Vector2 screenPosition = Input.mousePosition;

        if (raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.Log("AR-Oberfläche erkannt.");

            Pose hitPose = hits[0].pose;
            Instantiate(prefabToPlace, hitPose.position, hitPose.rotation);
        }
        else
        {
            Debug.Log("Linksklick, aber keine AR-Oberfläche erkannt.");
        }
    }
}
}