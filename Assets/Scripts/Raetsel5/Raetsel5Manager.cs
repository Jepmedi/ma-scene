using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Chef d'orchestre de la scène du chat.
// Gère le bandeau de messages en haut de l'écran, la barre des ingrédients
// à trouver (avec coche verte), et l'enchaînement du rätsel.
public class Raetsel5Manager : MonoBehaviour
{
    public static Raetsel5Manager Instance { get; private set; }

    // Un ingrédient à trouver : son icône dans le HUD + son objet dans la scène.
    [Serializable]
    public class Ingredient
    {
        public string label = "Zutat";
        [Tooltip("Icône affichée en haut de l'écran (une simple image PNG suffit).")]
        public Texture icon;
        [Tooltip("L'objet à ramasser dans la scène. Son numéro est attribué " +
                 "automatiquement : la coche tombera toujours sur la bonne icône.")]
        public IngredientItem item;
        [HideInInspector] public bool found;
    }

    [Header("Objets de la scène")]
    public CryingCat cat;

    [Tooltip("Les placeurs des 3 ingrédients : ils se déclenchent seulement " +
             "une fois que le joueur a compris que le chat a faim.")]
    public PlaceAroundPlayer[] ingredientPlacers;

    [Header("Les 3 ingrédients à trouver")]
    public Ingredient[] ingredients = new Ingredient[3];

    [Header("Fin : cuisson, repas et récompense")]
    [Tooltip("Le plat servi au chat (ex: Bowl_001). Caché au départ.")]
    public GameObject meal;
    [Tooltip("Hauteur réelle du plat, en mètres.")]
    public float mealHeight = 0.08f;
    public AudioClip cookingSound;
    [Tooltip("Durée de la cuisson, en secondes.")]
    public float cookingDuration = 3f;
    [Tooltip("Le chiffre du code donné par ce rätsel.")]
    public string code = "9";

    [Header("Messages (allemand, comme le mock)")]
    [TextArea] public string searchMessage = "Du hörst ein leises Weinen...";
    [TextArea] public string hungryMessage = "Die Katze hat Hunger.\nDu musst etwas für sie kochen.";
    [TextArea] public string findIngredientsMessage = "Suche die Zutaten,\ndie du brauchst (3).";
    [TextArea] public string cookMessage = "Geh zurück zur Küche und\ntippe auf den Herd.";
    [TextArea] public string needIngredientsMessage = "Dir fehlen noch Zutaten!";
    [TextArea] public string cookingMessage = "Du kochst für die Katze...";
    [TextArea] public string successMessage = "Die Katze ist satt und glücklich!\nHier ist dein Code:";

    private Text _message;
    private RectTransform _ingredientBar;
    private RawImage[] _icons;
    private GameObject[] _checks;
    private bool _catExamined = false;
    private bool _cooked = false;
    private GameObject _codePanel;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Sécurité : si le tableau n'a pas été rempli dans l'Inspector,
        // on évite de tout faire planter.
        if (ingredients == null || ingredients.Length == 0)
        {
            Debug.LogWarning("[Raetsel5] Le tableau 'Ingredients' est vide dans l'Inspector " +
                             "-> mets sa taille à 3 et assigne les icônes.");
            ingredients = new Ingredient[3];
            for (int i = 0; i < 3; i++) ingredients[i] = new Ingredient();
        }
        for (int i = 0; i < ingredients.Length; i++)
            if (ingredients[i] == null) ingredients[i] = new Ingredient();

        // Si le chat n'a pas été renseigné dans l'Inspector, on le retrouve tout seul
        // (sinon il continuerait de pleurer même après avoir été nourri).
        if (cat == null)
        {
            cat = FindAnyObjectByType<CryingCat>(FindObjectsInactive.Include);
            if (cat != null)
                Debug.Log("[Raetsel5] Champ 'Cat' vide -> chat retrouvé automatiquement : " + cat.name);
            else
                Debug.LogWarning("[Raetsel5] Aucun CryingCat trouvé dans la scène.");
        }

        // Chaque ingrédient reçoit automatiquement le numéro de sa ligne :
        // impossible que la coche se trompe d'icône.
        for (int i = 0; i < ingredients.Length; i++)
        {
            if (ingredients[i].item != null)
            {
                ingredients[i].item.index = i;
                Debug.Log($"[Raetsel5] '{ingredients[i].item.name}' -> icône {i} ({ingredients[i].label})");
            }
        }

        CreateUI();
    }

    // ────────────────────────────── UI ──────────────────────────────

    void CreateUI()
    {
        GameObject canvasObj = new GameObject("Raetsel5Canvas");
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 900;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        CreateMessagePanel(canvasObj.transform);
        ShowMessage(searchMessage);          // le message d'abord : il ne dépend de rien d'autre
        CreateIngredientBar(canvasObj.transform);
    }

    // Charge une police pour l'UI. Sans police, un composant Text est
    // totalement invisible (sans erreur), d'où les secours successifs.
    static Font GetUIFont()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Arial", 40);
        if (f == null) f = Font.CreateDynamicFontFromOSFont("Segoe UI", 40);

        if (f == null)
            Debug.LogError("[Raetsel5] Aucune police chargée -> le texte restera invisible.");
        else
            Debug.Log("[Raetsel5] Police utilisée pour l'UI : " + f.name);

        return f;
    }

    void CreateMessagePanel(Transform parent)
    {
        GameObject panelObj = new GameObject("MessagePanel", typeof(RectTransform));
        panelObj.GetComponent<RectTransform>().SetParent(parent, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.55f);
        panel.raycastTarget = false;
        RectTransform pr = panel.rectTransform;
        pr.anchorMin = new Vector2(0.06f, 0.76f);
        pr.anchorMax = new Vector2(0.94f, 0.88f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("MessageText", typeof(RectTransform));
        textObj.GetComponent<RectTransform>().SetParent(panelObj.transform, false);
        _message = textObj.AddComponent<Text>();
        _message.font = GetUIFont();
        _message.alignment = TextAnchor.MiddleCenter;
        _message.color = Color.white;
        _message.raycastTarget = false;
        // Le texte s'adapte tout seul à la largeur du bandeau : plus de phrase coupée.
        _message.horizontalOverflow = HorizontalWrapMode.Wrap;
        _message.verticalOverflow = VerticalWrapMode.Truncate;
        _message.resizeTextForBestFit = true;
        _message.resizeTextMinSize = 12;
        _message.resizeTextMaxSize = 44;
        RectTransform tr = _message.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(20, 8);
        tr.offsetMax = new Vector2(-20, -8);
    }

    // Barre transparente des ingrédients, sous le message. Cachée au départ.
    void CreateIngredientBar(Transform parent)
    {
        GameObject barObj = new GameObject("IngredientBar", typeof(RectTransform));
        barObj.GetComponent<RectTransform>().SetParent(parent, false);
        Image bg = barObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);   // fond translucide
        bg.raycastTarget = false;
        _ingredientBar = bg.rectTransform;
        _ingredientBar.anchorMin = new Vector2(0.16f, 0.62f);
        _ingredientBar.anchorMax = new Vector2(0.84f, 0.745f);
        _ingredientBar.offsetMin = Vector2.zero;
        _ingredientBar.offsetMax = Vector2.zero;

        int n = Mathf.Max(1, ingredients.Length);
        _icons = new RawImage[n];
        _checks = new GameObject[n];

        for (int i = 0; i < n; i++)
        {
            float x0 = i / (float)n;
            float x1 = (i + 1) / (float)n;

            GameObject slot = new GameObject("Slot" + i, typeof(RectTransform));
            RectTransform sr = slot.GetComponent<RectTransform>();
            sr.SetParent(barObj.transform, false);
            sr.anchorMin = new Vector2(x0, 0f);
            sr.anchorMax = new Vector2(x1, 1f);
            sr.offsetMin = new Vector2(8, 6);
            sr.offsetMax = new Vector2(-8, -6);

            // L'icône de l'ingrédient (RawImage : accepte une texture PNG telle quelle)
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.GetComponent<RectTransform>().SetParent(slot.transform, false);
            RawImage icon = iconObj.AddComponent<RawImage>();
            icon.raycastTarget = false;
            icon.texture = (i < ingredients.Length) ? ingredients[i].icon : null;
            icon.color = new Color(1f, 1f, 1f, 0.45f);   // grisé tant que non trouvé
            RectTransform ir = icon.rectTransform;
            ir.anchorMin = new Vector2(0.15f, 0.30f);
            ir.anchorMax = new Vector2(0.85f, 1f);
            ir.offsetMin = Vector2.zero;
            ir.offsetMax = Vector2.zero;
            _icons[i] = icon;

            // La coche verte, sous l'icône, cachée au départ
            _checks[i] = CreateCheckmark(slot.transform);
            _checks[i].SetActive(false);
        }

        barObj.SetActive(false);   // apparaît quand le chat a été examiné
    }

    // Coche verte dessinée avec deux barres (aucune image nécessaire).
    GameObject CreateCheckmark(Transform parent)
    {
        GameObject check = new GameObject("Check", typeof(RectTransform));
        RectTransform cr = check.GetComponent<RectTransform>();
        cr.SetParent(parent, false);
        cr.anchorMin = new Vector2(0.5f, 0f);
        cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(0.5f, 0f);
        cr.anchoredPosition = new Vector2(0f, 2f);
        cr.sizeDelta = new Vector2(44f, 34f);

        Color green = new Color(0.25f, 0.85f, 0.35f, 1f);

        // Petite branche (bas-gauche)
        GameObject a = new GameObject("ArmA", typeof(RectTransform));
        a.GetComponent<RectTransform>().SetParent(check.transform, false);
        Image ia = a.AddComponent<Image>();
        ia.color = green;
        ia.raycastTarget = false;
        RectTransform ar = ia.rectTransform;
        ar.sizeDelta = new Vector2(18f, 7f);
        ar.anchoredPosition = new Vector2(-11f, 12f);
        ar.localRotation = Quaternion.Euler(0f, 0f, -45f);

        // Grande branche (haut-droite)
        GameObject b = new GameObject("ArmB", typeof(RectTransform));
        b.GetComponent<RectTransform>().SetParent(check.transform, false);
        Image ib = b.AddComponent<Image>();
        ib.color = green;
        ib.raycastTarget = false;
        RectTransform br = ib.rectTransform;
        br.sizeDelta = new Vector2(32f, 7f);
        br.anchoredPosition = new Vector2(4f, 17f);
        br.localRotation = Quaternion.Euler(0f, 0f, 45f);

        return check;
    }

    public void ShowMessage(string text)
    {
        if (_message != null) _message.text = text;
        Debug.Log("[Raetsel5] " + text);
    }

    // ────────────────────────── Étapes du rätsel ──────────────────────────

    // Appelé par CryingCat quand le joueur touche le chat.
    public void OnCatExamined()
    {
        if (_catExamined) return;
        _catExamined = true;

        ShowMessage(hungryMessage);

        // On affiche la barre des ingrédients à trouver.
        if (_ingredientBar != null) _ingredientBar.gameObject.SetActive(true);
        Invoke(nameof(ShowFindIngredients), 2.5f);
    }

    void ShowFindIngredients()
    {
        ShowMessage(findIngredientsMessage);

        // Les ingrédients apparaissent maintenant dans la pièce.
        if (ingredientPlacers != null)
        {
            foreach (PlaceAroundPlayer placer in ingredientPlacers)
                if (placer != null) placer.Begin();

            Debug.Log($"[Raetsel5] {ingredientPlacers.Length} ingrédient(s) dispersé(s) dans la pièce.");
        }
    }

    // Appelé quand le joueur touche un ingrédient dans la scène (étape 4).
    public void OnIngredientFound(int index)
    {
        if (index < 0 || index >= ingredients.Length) return;
        if (ingredients[index].found) return;

        ingredients[index].found = true;

        // L'icône passe en pleine couleur et la coche verte apparaît.
        if (_icons != null && index < _icons.Length && _icons[index] != null)
            _icons[index].color = Color.white;
        if (_checks != null && index < _checks.Length && _checks[index] != null)
            _checks[index].SetActive(true);

        Debug.Log("[Raetsel5] Ingrédient trouvé : " + ingredients[index].label);

        if (AllIngredientsFound())
        {
            ShowMessage(cookMessage);
            Debug.Log("[Raetsel5] Tous les ingrédients sont réunis -> direction le fourneau.");
        }
    }

    public bool AllIngredientsFound()
    {
        foreach (Ingredient ing in ingredients)
            if (!ing.found) return false;
        return true;
    }

    // Appelé par CookingStove quand le joueur touche la cuisinière.
    public void OnStoveTapped()
    {
        if (_cooked) return;

        if (!AllIngredientsFound())
        {
            ShowMessage(needIngredientsMessage);
            return;
        }

        _cooked = true;
        StartCoroutine(CookAndServe());
    }

    IEnumerator CookAndServe()
    {
        ShowMessage(cookingMessage);

        if (cookingSound != null && cat != null)
            AudioSource.PlayClipAtPoint(cookingSound, cat.transform.position);

        yield return new WaitForSeconds(cookingDuration);

        // D'ABORD faire taire le chat : c'est le plus important, et ça ne doit
        // dépendre d'aucune autre étape (si le placement du plat échouait,
        // le chat continuerait de pleurer indéfiniment).
        if (cat != null)
            cat.Calm();
        else
            Debug.LogWarning("[Raetsel5] Le champ 'Cat' est vide dans l'Inspector : " +
                             "le chat continuera de pleurer après le repas !");

        ServeMeal();

        ShowMessage(successMessage + " " + code);
        ShowCodePanel();

        Debug.Log("[Raetsel5] Rätsel résolu -> code : " + code);
    }

    // Fait apparaître le plat juste devant le chat, posé au sol.
    void ServeMeal()
    {
        if (meal == null || cat == null) return;

        // Devant le chat, du côté du joueur (pour qu'on le voie bien).
        Vector3 offset = Vector3.forward;
        if (Camera.main != null)
        {
            offset = Camera.main.transform.position - cat.transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 0.0001f) offset.Normalize();
        }

        float floorY = cat.transform.position.y;
        meal.transform.position = cat.transform.position + offset * 0.35f;
        meal.SetActive(true);

        // Taille réelle + calage au sol.
        ScaleAndDrop(meal, mealHeight, floorY);
    }

    static void ScaleAndDrop(GameObject obj, float targetHeight, float floorY)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        if (targetHeight > 0f && b.size.y > 0.0001f)
            obj.transform.localScale *= targetHeight / b.size.y;

        // Recalcul après mise à l'échelle, puis on pose le bas sur le sol.
        rends = obj.GetComponentsInChildren<Renderer>(true);
        b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
        obj.transform.position += new Vector3(0f, floorY - b.min.y + 0.01f, 0f);
    }

    // Grand affichage du code, comme dans le mock.
    void ShowCodePanel()
    {
        if (_codePanel != null) { _codePanel.SetActive(true); return; }

        Transform canvas = _message != null ? _message.canvas.transform : null;
        if (canvas == null) return;

        GameObject panelObj = new GameObject("CodePanel", typeof(RectTransform));
        panelObj.GetComponent<RectTransform>().SetParent(canvas, false);
        Image bg = panelObj.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);
        bg.raycastTarget = false;
        RectTransform pr = bg.rectTransform;
        pr.anchorMin = new Vector2(0.35f, 0.40f);
        pr.anchorMax = new Vector2(0.65f, 0.58f);
        pr.offsetMin = Vector2.zero;
        pr.offsetMax = Vector2.zero;

        GameObject textObj = new GameObject("CodeText", typeof(RectTransform));
        textObj.GetComponent<RectTransform>().SetParent(panelObj.transform, false);
        Text t = textObj.AddComponent<Text>();
        t.font = GetUIFont();
        t.text = code;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.85f, 0.3f);
        t.raycastTarget = false;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 20;
        t.resizeTextMaxSize = 200;
        RectTransform tr = t.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(10, 10);
        tr.offsetMax = new Vector2(-10, -10);

        _codePanel = panelObj;
    }

#if UNITY_EDITOR
    // Test rapide dans l'éditeur : touches 1, 2, 3 pour simuler la découverte
    // des ingrédients sans avoir encore à les chercher dans la scène.
    void Update()
    {
        if (!_catExamined) return;
        if (Input.GetKeyDown(KeyCode.Alpha1)) OnIngredientFound(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) OnIngredientFound(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) OnIngredientFound(2);
    }
#endif
}
