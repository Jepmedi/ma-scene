using System.Collections;
using UnityEngine;

// À mettre sur le grimoire. Quand le joueur tape dessus, le code caché
// (ex: "2") apparaît au-dessus des pages avec un effet d'apparition
// magique (grossissement + fondu, couleur dorée).
public class TapToOpen : MonoBehaviour
{
    [Tooltip("Le chiffre du code caché dans le grimoire.")]
    public string code = "2";

    [Tooltip("Taille du chiffre. Augmente si le code est trop petit, diminue s'il est trop gros.")]
    public float codeCharacterSize = 0.1f;

    [Tooltip("Couleur du code révélé.")]
    public Color codeColor = new Color(1f, 0.85f, 0.3f); // doré

    [Tooltip("Durée de l'apparition (secondes).")]
    public float revealDuration = 0.6f;

    private bool _opened = false;

    void Update()
    {
        if (_opened) return;

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
        if (!tapped) return;

        // Rayon depuis l'écran : s'il touche CE grimoire, c'est un tap dessus.
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.transform.IsChildOf(transform))
        {
            _opened = true;
            StartCoroutine(RevealCode());
        }
    }

    IEnumerator RevealCode()
    {
        Debug.Log("[TapToOpen] Grimoire ouvert -> code révélé : " + code);

        // Centre / haut du livre (via les limites de son maillage).
        Renderer rend = GetComponentInChildren<Renderer>();
        Vector3 center = (rend != null) ? rend.bounds.center : transform.position;
        float top = (rend != null) ? rend.bounds.max.y : transform.position.y;
        float bookSize = (rend != null) ? rend.bounds.size.magnitude : 0.3f;

        // Le texte du code.
        GameObject label = new GameObject("Code");
        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = code;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = codeCharacterSize;
        tm.fontSize = 90;
        tm.color = codeColor;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MeshRenderer mr = label.GetComponent<MeshRenderer>();
        if (tm.font != null) mr.material = tm.font.material;

        // Juste au-dessus des pages, tourné vers la caméra.
        label.transform.position = new Vector3(center.x, top + bookSize * 0.15f, center.z);
        if (Camera.main != null)
            label.transform.rotation = Quaternion.LookRotation(
                label.transform.position - Camera.main.transform.position);

        // Apparition : grossit (avec un léger rebond) et se colore progressivement.
        float t = 0f;
        Color c = codeColor;
        while (t < revealDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / revealDuration);
            label.transform.localScale = Vector3.one * EaseOutBack(k);
            c.a = k;
            tm.color = c;
            yield return null;
        }
        label.transform.localScale = Vector3.one;
        c.a = 1f;
        tm.color = c;
    }

    // Grossissement avec un petit dépassement (effet "pop" magique).
    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }
}
