using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Auf dasselbe Objekt wie den ARPlaneManager legen (das XR Origin).
// Lässt das Telefon an einer ZUFÄLLIGEN Stelle auf einer erkannten horizontalen Ebene erscheinen.
[RequireComponent(typeof(ARPlaneManager))]
public class PhoneARPlacer : MonoBehaviour
{
    [Header("Zu platzierendes Objekt")]
    public GameObject phone;              // das 'phone'-Objekt (mit PhoneRinging)

    [Header("Platzierungs-Einschränkungen")]
    public float minPlaneSize = 0.2f;     // Mindestgröße der horizontalen Ebene (Meter)
    public float placementYOffset = 0f;   // vertikaler Versatz, falls das Telefon einsinkt/schwebt

    [Header("Test im Editor")]
    public bool editorFallback = true;    // wenn keine Ebene (im Editor), vor der Kamera platzieren
    public float editorFallbackDelay = 2f;

    private ARPlaneManager _planeManager;
    private bool _placed = false;

    void Awake()
    {
        _planeManager = GetComponent<ARPlaneManager>();
        if (phone != null)
            phone.SetActive(false);       // Telefon verstecken, solange keine Ebene gefunden ist
    }

    void Update()
    {
        if (_placed) return;

        // --- Sucht eine HORIZONTALE Ebene (nach oben) mit ausreichender Größe ---
        foreach (ARPlane plane in _planeManager.trackables)
        {
            if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
            if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize) continue;

            // Zufälliger Punkt innerhalb der Grenzen der Ebene.
            Vector2 ext = plane.size * 0.5f;
            Vector3 local = new Vector3(Random.Range(-ext.x, ext.x), 0f, Random.Range(-ext.y, ext.y));
            Vector3 world = plane.transform.TransformPoint(local);

            PlacePhone(world);
            return;
        }

#if UNITY_EDITOR
        // Im Editor gibt es keine echten Ebenen: zum Testen vor der Kamera platzieren.
        if (editorFallback && Time.timeSinceLevelLoad > editorFallbackDelay)
        {
            Transform cam = Camera.main.transform;
            PlacePhone(cam.position + cam.forward * 1.5f + Vector3.down * 0.3f);
        }
#endif
    }

    void PlacePhone(Vector3 pos)
    {
        if (phone == null) { Debug.LogError("[PhoneARPlacer] 'phone' ist nicht zugewiesen!"); return; }

        _placed = true;
        phone.transform.position = pos + Vector3.up * placementYOffset;

        // Richtet das Telefon aufrecht zur Kamera aus (ohne es zu kippen).
        Vector3 toCam = Camera.main.transform.position - phone.transform.position;
        toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            phone.transform.rotation = Quaternion.LookRotation(toCam);

        phone.SetActive(true);            // löst PhoneRinging aus (Klingeln)
        Debug.Log("[PhoneARPlacer] Telefon auf einer horizontalen Ebene platziert.");

        // Optional: die Ebenen ausblenden, sobald das Telefon platziert ist.
        // foreach (ARPlane p in _planeManager.trackables) p.gameObject.SetActive(false);
    }
}
