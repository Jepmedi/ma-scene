using UnityEngine;

// À mettre sur la cuisinière. Quand le joueur la touche, prévient le manager :
// - s'il manque des ingrédients, un message le lui rappelle ;
// - s'il les a tous, la cuisson démarre.
public class CookingStove : MonoBehaviour
{
    [Header("Zone de clic")]
    [Tooltip("Agrandit la zone tactile autour de la cuisinière.")]
    public float tapTargetScale = 1.2f;

    void Start()
    {
        EnsureCollider();
    }

    void Update()
    {
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

        // RaycastAll : le plan AR a un MeshCollider qui intercepterait le clic.
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(transform)) continue;

            Debug.Log("[CookingStove] Cuisinière touchée.");
            if (Raetsel5Manager.Instance != null)
                Raetsel5Manager.Instance.OnStoveTapped();
            else
                Debug.LogWarning("[CookingStove] Aucun Raetsel5Manager dans la scène.");
            return;
        }
    }

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
