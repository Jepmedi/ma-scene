using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Attend qu'un plan horizontal (le sol) soit détecté par l'AR,
// puis pose l'objet dessus. Une seule fois.
public class PlaceOnPlane : MonoBehaviour
{
    [Tooltip("Le AR Plane Manager (composant qui est sur XR Origin).")]
    public ARPlaneManager planeManager;

    [Tooltip("L'objet à poser sur le sol.")]
    public GameObject objectToPlace;

    [Tooltip("Taille minimale du plan (en mètres) pour l'accepter.")]
    public float minPlaneSize = 0.2f;

    [Tooltip("Hauteur ajoutée au-dessus du sol (m). 0 = posé à plat.")]
    public float heightOffset = 0.01f;

    [Tooltip("Si coché, place l'objet à un endroit ALÉATOIRE du plan (sinon au centre).")]
    public bool randomPosition = true;

    [Tooltip("Si coché, se déclenche dès le début. Sinon, attend un appel à Begin().")]
    public bool autoStart = true;

    private bool _placed = false;
    private bool _started = false;

    void Start()
    {
        _started = autoStart;
    }

    // À appeler pour lancer le placement quand autoStart est décoché.
    public void Begin()
    {
        _started = true;
    }

    void Update()
    {
        if (!_started || _placed || planeManager == null || objectToPlace == null) return;

        foreach (ARPlane plane in planeManager.trackables)
        {
            // On veut seulement le sol (plan horizontal tourné vers le haut).
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize) continue;

            // Point de pose : centre du plan, ou un point aléatoire à l'intérieur.
            Vector3 pos;
            if (randomPosition)
            {
                Vector2 ext = plane.size * 0.5f;
                Vector3 local = new Vector3(
                    Random.Range(-ext.x, ext.x), 0f, Random.Range(-ext.y, ext.y));
                pos = plane.transform.TransformPoint(local);
            }
            else
            {
                pos = plane.transform.position;
            }
            pos.y += heightOffset;

            objectToPlace.transform.position = pos;
            objectToPlace.SetActive(true);
            _placed = true;

            Debug.Log($"[PlaceOnPlane] {objectToPlace.name} posé. " +
                      $"Plan utilisé : {plane.size.x:F1} x {plane.size.y:F1} m " +
                      $"(hauteur {plane.transform.position.y:F2} m). " +
                      $"Plus le plan est grand, plus la position varie.");
            return;
        }
    }
}
