using UnityEngine;

public class Billboard : MonoBehaviour
{
    void Update()
    {
        
        transform.LookAt(Camera.main.transform);

        
        transform.Rotate(0, 180, 0);
    }
}