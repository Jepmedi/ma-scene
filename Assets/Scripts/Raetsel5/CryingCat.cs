using System.Collections;
using UnityEngine;

// À mettre sur le chat. Dès qu'il apparaît, il miaule (animation "miau" rejouée
// en boucle) et émet un son de pleur SPATIAL : le joueur le retrouve à l'oreille.
// Quand il est nourri, on appelle Calm() : il se tait et repasse en animation calme.
public class CryingCat : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Animator du chat (laisser vide : détecté automatiquement).")]
    public Animator animator;
    [Tooltip("Animation jouée entre deux miaulements (le chat attend, assis).")]
    public string idleAnimation = "sitting";
    [Tooltip("Animation du miaulement.")]
    public string cryAnimation = "miau";
    [Tooltip("Animation quand le chat est nourri et content.")]
    public string calmAnimation = "idle";
    [Tooltip("Temps d'attente entre deux miaulements (secondes).")]
    public float meowInterval = 2.5f;

    [Header("Taille")]
    [Tooltip("Hauteur voulue du chat en mètres, quelle que soit l'échelle du FBX. " +
             "0.35 = chat réaliste, 0.5 = bien visible pour le jeu. 0 = ne pas y toucher.")]
    public float targetHeight = 0.45f;

    [Header("Zone de clic")]
    [Tooltip("Agrandit la zone tactile autour du chat pour qu'il soit facile à toucher. " +
             "1 = taille exacte du chat, 2 = deux fois plus grand.")]
    public float tapTargetScale = 1.8f;

    [Header("Son de pleur (spatial, en boucle)")]
    public AudioClip cryClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Distance au-delà de laquelle on n'entend plus le chat.")]
    public float maxDistance = 15f;

    private AudioSource _audio;
    private bool _calmed = false;
    private bool _examined = false;

    void Start()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("[CryingCat] Aucun Animator trouvé sur le chat : il ne s'animera pas.");
        }
        else
        {
            // Sans ça, l'Animator cesse d'animer dès qu'il se croit hors champ.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Debug.Log("[CryingCat] Animator trouvé : " + animator.name);
        }

        ApplyTargetHeight();   // d'abord la taille...
        EnsureCollider();      // ...puis la zone de clic, calculée dessus

        if (Camera.main == null)
            Debug.LogWarning("[CryingCat] Aucune caméra taguée 'MainCamera' : le clic sur le chat ne marchera pas.");

        StartCoroutine(MeowLoop());
        PlayCrySound();

        // Diagnostic : s'il y a plus d'un AudioSource, un son posé à la main
        // pourrait continuer à jouer indépendamment du script.
        int n = GetComponentsInChildren<AudioSource>(true).Length;
        if (n > 1)
            Debug.LogWarning($"[CryingCat] {n} AudioSource sur le chat. " +
                             "Si l'un a 'Play On Awake' coché, il jouera en double.");
    }

    // Met le chat à une taille réelle en mètres, quelle que soit l'échelle
    // à laquelle son FBX a été exporté (souvent 10x ou 100x à côté).
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

        Debug.Log($"[CryingCat] Taille du chat : {current:F2} m -> {targetHeight:F2} m (x{k:F2})");
    }

    // Le chat est petit : on lui donne une zone tactile généreuse, plus large
    // que son corps, pour qu'il soit facile à toucher (surtout sur téléphone).
    void EnsureCollider()
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length == 0)
        {
            Debug.LogWarning("[CryingCat] Pas de Renderer : impossible de créer une zone de clic.");
            return;
        }

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

        Debug.Log($"[CryingCat] Zone de clic ajustée (x{k}) autour du chat.");
    }

    // Le chat attend assis, puis miaule, puis se rassoit... tant qu'il n'est pas nourri.
    IEnumerator MeowLoop()
    {
        while (!_calmed)
        {
            // Miaulement
            if (animator != null && !string.IsNullOrEmpty(cryAnimation))
            {
                animator.Play(cryAnimation, 0, 0f);
                yield return null; // laisse l'Animator entrer dans l'état
                float len = animator.GetCurrentAnimatorStateInfo(0).length;
                yield return new WaitForSeconds(Mathf.Max(0.2f, len));
            }

            if (_calmed) break;

            // Il se rassoit et attend
            if (animator != null && !string.IsNullOrEmpty(idleAnimation))
                animator.Play(idleAnimation, 0, 0f);

            yield return new WaitForSeconds(meowInterval);
        }
    }

    // Le joueur touche le chat -> on découvre qu'il a faim.
    void Update()
    {
        if (_examined) return;

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
        // intercepterait le clic avant le chat.
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(transform)) continue;

            _examined = true;
            Debug.Log("[CryingCat] Le chat a été touché : il a faim.");

            if (Raetsel5Manager.Instance != null)
                Raetsel5Manager.Instance.OnCatExamined();
            else
                Debug.LogWarning("[CryingCat] Aucun Raetsel5Manager dans la scène : pas de message affiché.");
            return;
        }
    }

    void PlayCrySound()
    {
        if (cryClip == null) return;

        _audio = gameObject.AddComponent<AudioSource>();
        _audio.clip = cryClip;
        _audio.loop = true;              // il pleure sans s'arrêter jusqu'à être nourri
        _audio.volume = volume;
        _audio.spatialBlend = 1f;        // 3D : le son vient de la position du chat
        _audio.rolloffMode = AudioRolloffMode.Linear;
        _audio.minDistance = 0.5f;
        _audio.maxDistance = maxDistance;
        _audio.Play();
    }

    // Le chat est nourri : il arrête de pleurer et redevient calme/content.
    public void Calm()
    {
        if (_calmed) return;
        _calmed = true;

        // Coupe net la boucle de miaulement (sans attendre la fin de son attente).
        StopAllCoroutines();

        // Coupe TOUS les sons du chat, pas seulement celui qu'on a créé :
        // s'il y avait un AudioSource posé à la main dans l'Inspector, il se
        // tairait sinon jamais.
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);
        foreach (AudioSource src in sources) src.Stop();

        if (animator != null && !string.IsNullOrEmpty(calmAnimation))
            animator.Play(calmAnimation, 0, 0f);

        Debug.Log($"[CryingCat] Le chat est nourri : il ne pleure plus " +
                  $"({sources.Length} source(s) audio coupée(s)).");
    }

    public bool IsCalmed => _calmed;
}
