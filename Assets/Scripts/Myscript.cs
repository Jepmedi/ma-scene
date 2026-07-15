using UnityEngine;
using System.Collections;

public class Myscript : MonoBehaviour
{
     public GameObject Cube;
     public GameObject table;
    public GameObject chair;
    public GameObject sofa;
   public  GameObject[] prefabs;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {

           // Erzeugt einen Strahl von der Kamera zur Maus
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            // Prüft, ob der Strahl ein Objekt trifft
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log("Objekt getroffen: " + hit.collider.gameObject.name);
                int index = Random.Range(0, prefabs.Length);
            Instantiate(sofa, hit.point, Quaternion.identity);
            }
            else
            {
                Debug.Log("Kein Objekt getroffen");
            }
        }
        if (Input.GetMouseButtonDown(1))
            Debug.Log("Rechtsklick gedrückt.");

        if (Input.GetMouseButtonDown(2))
            Debug.Log("Mittelklick gedrückt.");
    }
}
