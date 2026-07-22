using System.Collections;
using UnityEngine;

// À mettre sur chaque ingrédient à trouver (boîte de thon, bol de riz, lait).
// Quand le joueur le touche : petite animation, l'ingrédient est ramassé,
// et la coche verte apparaît sur son icône en haut de l'écran.
public class IngredientItem : MonoBehaviour
{
    // Numéro de l'icône correspondante dans le HUD.
    // Attribué AUTOMATIQUEMENT par le Raetsel5Manager selon la ligne où
    // l'objet est glissé : rien à régler à la main ici.
    [HideInInspector] public int index = 0;

    [Header("Taille")]
    [Tooltip("Hauteur réelle voulue, en mètres. Boîte de conserve ~0.10, " +
             "bol ~0.08, brique de lait ~0.20. (0 = ne pas y toucher.)")]
    public float targetHeight = 0.12f;

    [Header("Zone de clic")]
    [Tooltip("Agrandit la zone tactile : ces objets sont petits et durs à viser.")]
    public float tapTargetScale = 2.5f;

    [Header("Ramassage")]
    public AudioClip pickupSound;
    [Tooltip("Durée de la petite animation de ramassage (secondes).")]
    public float pickupTime = 0.35f;

    private bool _found = false;

    void Awake()
    {
        ApplyTargetHeight();
    }

    void Start()
    {
        EnsureCollider();
    }

    void Update()
    {
        if (_found) return;

        Vector2 screenPos = Vector2.zero;
        bool tapped = false;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) { screenPos = Input.mousePosition; tapped = true; }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            screenPos = Input.GetTouch(0).position;
            tapped = true;
        }
#endif
        if (!tapped || Camera.main == null) return;

        // RaycastAll et non Raycast : le plan AR a son propre MeshCollider et
        // intercepterait le clic avant l'objet posé dessus.
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
            {
                _found = true;
                StartCoroutine(Collect());
                return;
            }
        }
    }

    IEnumerator Collect()
    {
        Debug.Log($"[IngredientItem] Ingrédient {index} ramassé : {name}");

        // On coche tout de suite dans le HUD.
        if (Raetsel5Manager.Instance != null)
            Raetsel5Manager.Instance.OnIngredientFound(index);
        else
            Debug.LogWarning("[IngredientItem] Aucun Raetsel5Manager dans la scène.");

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // Petite animation : l'objet monte et rétrécit avant de disparaître.
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < pickupTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / pickupTime);
            transform.position = startPos + Vector3.up * (0.25f * k);
            transform.localScale = startScale * (1f - k);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    // Donne à l'objet sa taille réelle, quelle que soit l'échelle du FBX.
    void ApplyTargetHeight()
    {
        if (targetHeight <= 0f) return;

        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float current = b.size.y;
        if (current < 0.0001f) return;

        float k = targetHeight / current;
        transform.localScale *= k;

        Debug.Log($"[IngredientItem] {name} : hauteur {current:F2} m -> {targetHeight:F2} m (x{k:F2})");
    }

    // Zone tactile élargie : ces objets sont petits.
    void EnsureCollider()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        BoxCollider bc = GetComponent<BoxCollider>();
        if (bc == null) bc = gameObject.AddComponent<BoxCollider>();

        bc.center = transform.InverseTransformPoint(b.center);

        Vector3 ls = transform.lossyScale;
        float k = Mathf.Max(1f, tapTargetScale);
        bc.size = new Vector3(
            b.size.x / Mathf.Max(0.0001f, Mathf.Abs(ls.x)),
            b.size.y / Mathf.Max(0.0001f, Mathf.Abs(ls.y)),
            b.size.z / Mathf.Max(0.0001f, Mathf.Abs(ls.z))) * k;
    }
}
