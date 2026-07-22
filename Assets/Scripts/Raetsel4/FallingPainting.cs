using System.Collections;
using UnityEngine;

// À mettre sur le tableau (un Quad). Le tableau apparaît DEBOUT sur le sol,
// face à la caméra (bien visible, ne se confond pas avec le plan AR), avec des
// débris à ses pieds et un bruit spatial de chute qui attire le joueur vers lui.
// Quand on tape dessus : le bruit s'arrête et l'indice s'affiche
// ("Schätze verstecken sich oft in Büchern"), puis (étape 9) le grimoire apparaît.
public class FallingPainting : MonoBehaviour
{
    [Header("Image du tableau")]
    public Texture paintingTexture;   // l'image visible sur le tableau (ex: l'image du livre)

    [Header("Son (spatial, joué une fois à la chute)")]
    public AudioClip fallSound;       // bruit de chute (à assigner) ; sa propre résonance fait le fondu
    public bool loopSound = false;    // false = joué une seule fois puis s'éteint tout seul

    [Header("Débris (éclats) au pied du tableau")]
    public int debrisCount = 28;
    public float debrisRadius = 0.6f;
    [Tooltip("Matériau des éclats (glisser 'glass_debris' pour un rendu verre).")]
    public Material debrisMaterial;
    [Tooltip("Couleur de secours si aucun matériau n'est assigné.")]
    public Color debrisColor = new Color(0.25f, 0.16f, 0.09f);

    [Header("Indice affiché au tap")]
    [TextArea] public string hintText = "Schätze verstecken\nsich oft\nin Büchern";
    public Color hintColor = new Color(1f, 0.95f, 0.8f);
    public float hintCharacterSize = 0.05f;

    [Header("Après l'indice : faire apparaître le grimoire (optionnel pour l'instant)")]
    public PlaceOnPlane grimoirePlacer;   // laissé vide à l'étape 8, rempli à l'étape 9
    public float readTime = 2.5f;         // temps de lecture avant la suite

    private bool _revealed = false;
    private AudioSource _audio;

    void Start()
    {
        // Le script tourne au moment où PlaceOnPlane pose le tableau : sa position
        // est alors au niveau du sol. On garde ce niveau pour les débris.
        Vector3 floorPos = transform.position;

        // Applique l'image sur le tableau + rend le Quad visible des deux faces.
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            if (paintingTexture != null) r.material.mainTexture = paintingTexture;
            r.material.SetFloat("_Cull", 0); // 0 = double face (URP/Lit)
        }

        // Debout, face à la caméra.
        FaceCameraUpright();

        // L'origine du Quad est en son centre -> on le remonte d'une demi-hauteur
        // pour que son bas repose sur le sol.
        transform.position = floorPos + Vector3.up * (transform.lossyScale.y * 0.5f);

        SpawnDebris(floorPos);
        PlaySound();
    }

    void Update()
    {
        if (_revealed) return;

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

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.transform.IsChildOf(transform))
        {
            _revealed = true;
            if (_audio != null) _audio.Stop();   // le bruit s'arrête quand on découvre le tableau
            StartCoroutine(RevealHint());
        }
    }

    // Oriente le tableau debout (vertical), sa FACE VISIBLE tournée vers la caméra.
    // La face visible d'un Quad est du côté -Z, donc son "forward" (+Z) doit pointer
    // à l'opposé de la caméra.
    void FaceCameraUpright()
    {
        Vector3 awayFromCam = Vector3.forward;
        if (Camera.main != null)
        {
            awayFromCam = transform.position - Camera.main.transform.position;
            awayFromCam.y = 0f;
        }
        if (awayFromCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(awayFromCam);
    }

    // Éclats de verre éparpillés au sol autour du tableau.
    // Chaque éclat est un vrai petit volume triangulaire irrégulier (pas un cube),
    // sans collider pour ne pas gêner les taps.
    void SpawnDebris(Vector3 floorCenter)
    {
        if (debrisCount <= 0) return;

        Material mat = debrisMaterial;
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = debrisColor;
        }

        GameObject container = new GameObject("PaintingDebris");
        container.transform.position = floorCenter;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject shard = new GameObject("Shard" + i);
            shard.transform.SetParent(container.transform, true);

            shard.AddComponent<MeshFilter>().sharedMesh = CreateShardMesh();
            shard.AddComponent<MeshRenderer>().sharedMaterial = mat;

            // Éparpillés autour du pied du tableau, un peu plus denses au centre.
            Vector2 dir = Random.insideUnitCircle.normalized;
            float dist = Mathf.Pow(Random.value, 0.7f) * debrisRadius;
            Vector2 off = dir * dist;
            shard.transform.position = floorCenter + new Vector3(off.x, 0.002f, off.y);

            // Posés à plat mais tous de travers, quelques-uns bien inclinés.
            shard.transform.rotation = Quaternion.Euler(
                Random.Range(-30f, 30f), Random.Range(0f, 360f), Random.Range(-30f, 30f));

            // Tailles variées : beaucoup de petits, quelques gros morceaux.
            float s = Random.Range(0.03f, 0.075f);
            if (Random.value < 0.15f) s *= 1.9f;
            shard.transform.localScale = new Vector3(s, s * Random.Range(0.7f, 1.1f), s);
        }
    }

    // Construit un petit éclat : triangle irrégulier extrudé en épaisseur.
    static Mesh CreateShardMesh()
    {
        Vector3[] base3 = new Vector3[3];
        float start = Random.Range(0f, 360f);
        for (int i = 0; i < 3; i++)
        {
            float ang = (start + i * 120f + Random.Range(-38f, 38f)) * Mathf.Deg2Rad;
            float rad = Random.Range(0.3f, 0.6f);
            base3[i] = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
        }

        float h = Random.Range(0.05f, 0.14f); // épaisseur de l'éclat

        Vector3[] v = new Vector3[6];
        for (int i = 0; i < 3; i++)
        {
            v[i] = base3[i] + Vector3.up * h;  // face du dessus
            v[i + 3] = base3[i];               // face du dessous
        }

        int[] tris =
        {
            0, 1, 2,          // dessus
            5, 4, 3,          // dessous
            0, 3, 4, 0, 4, 1, // côtés
            1, 4, 5, 1, 5, 2,
            2, 5, 3, 2, 3, 0
        };

        Mesh m = new Mesh();
        m.vertices = v;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    void PlaySound()
    {
        if (fallSound == null) return;
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.clip = fallSound;
        _audio.loop = loopSound;
        _audio.spatialBlend = 1f;           // 3D : le son vient de la position du tableau
        _audio.rolloffMode = AudioRolloffMode.Linear;
        _audio.minDistance = 0.3f;
        _audio.maxDistance = 12f;
        _audio.Play();
    }

    IEnumerator RevealHint()
    {
        Debug.Log("[FallingPainting] Tableau examiné -> affichage de l'indice.");

        ShowHint();

        yield return new WaitForSeconds(readTime);

        if (grimoirePlacer != null)
            grimoirePlacer.Begin();
    }

    void ShowHint()
    {
        Renderer r = GetComponentInChildren<Renderer>();
        Vector3 center = (r != null) ? r.bounds.center : transform.position;
        float top = (r != null) ? r.bounds.max.y : transform.position.y;

        GameObject label = new GameObject("Hint");
        TextMesh tm = label.AddComponent<TextMesh>();
        tm.text = hintText;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = hintCharacterSize;
        tm.fontSize = 90;
        tm.color = hintColor;
        tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        MeshRenderer mr = label.GetComponent<MeshRenderer>();
        if (tm.font != null) mr.material = tm.font.material;

        label.transform.position = new Vector3(center.x, top + 0.1f, center.z);
        if (Camera.main != null)
            label.transform.rotation = Quaternion.LookRotation(
                label.transform.position - Camera.main.transform.position);
    }
}
