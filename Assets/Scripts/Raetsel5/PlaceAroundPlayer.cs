using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Fait apparaître un objet à une position ALÉATOIRE AUTOUR DU JOUEUR,
// posé au niveau du sol.
//
// Différence avec PlaceOnPlane : on ne place pas "dans le plan détecté"
// (qui est minuscule au début et fait surgir l'objet n'importe où),
// on attend juste de connaître la HAUTEUR du sol, puis on choisit une
// direction et une distance au hasard autour de la caméra.
// Résultat : position vraiment variée, toujours à portée, toujours au sol.
public class PlaceAroundPlayer : MonoBehaviour
{
    [Header("Références")]
    public ARPlaneManager planeManager;
    public GameObject objectToPlace;

    public enum Mode
    {
        AutourDuJoueur,   // dans un anneau autour de la caméra
        SurToutLePlan,    // n'importe où sur la surface scannée
        DansUnCoin        // dans un angle de la pièce, dos au mur
    }

    [Header("Où poser l'objet")]
    [Tooltip("AutourDuJoueur = à une distance donnée de toi.\n" +
             "SurToutLePlan = n'importe où sur le sol scanné (pour cacher le chat).\n" +
             "DansUnCoin = dans un angle de la pièce, face à la pièce (pour la cuisinière).")]
    public Mode mode = Mode.AutourDuJoueur;

    [Header("Mode DansUnCoin")]
    [Tooltip("De combien rentrer vers l'intérieur depuis le coin, en mètres.")]
    public float cornerInset = 0.5f;

    [Tooltip("Oriente l'objet vers le centre de la pièce (dos au mur).")]
    public bool faceRoomCenter = true;

    [Tooltip("Marge gardée par rapport au bord du plan, en mètres. " +
             "(Mode SurToutLePlan uniquement.)")]
    public float edgeMargin = 0.5f;

    [Tooltip("Coché = direction au hasard (pour le chat). " +
             "Décoché = toujours la même direction (pour la cuisinière). " +
             "(Mode AutourDuJoueur uniquement.)")]
    public bool randomDirection = true;

    [Tooltip("Direction fixe, en degrés, par rapport au regard du joueur. " +
             "0 = droit devant lui, 90 = à sa droite. (Utilisé si Random Direction est décoché.)")]
    public float fixedAngle = 0f;

    [Tooltip("Distance minimale au joueur, en mètres.")]
    public float minDistance = 1.5f;
    [Tooltip("Distance maximale au joueur, en mètres. " +
             "Mets la même valeur que Min pour une distance fixe.")]
    public float maxDistance = 3f;

    [Header("Détection du sol")]
    [Tooltip("Attente après détection du sol, le temps que l'AR se stabilise.")]
    public float startDelay = 2f;
    [Tooltip("Taille minimale d'un plan pour être considéré comme le sol.")]
    public float minPlaneSize = 0.5f;

    [Tooltip("Surface minimale scannée (m²) avant de poser l'objet. C'est LA valeur " +
             "qui évite qu'il apparaisse toujours au même endroit : tant que peu de " +
             "sol est scanné, il n'y a pas d'autre choix que le coin près de toi. " +
             "6 = il faut avoir balayé un vrai bout de pièce.")]
    public float minScannedArea = 6f;

    [Tooltip("Petite surélévation au-dessus du sol (m). Évite que l'objet s'enfonce " +
             "dans le plan (et devienne impossible à cliquer).")]
    public float heightOffset = 0.02f;

    [Tooltip("Si coché, se déclenche dès le début. Sinon, attend un appel à Begin().")]
    public bool autoStart = true;

    private bool _placed = false;
    private bool _started = false;
    private float _timer = 0f;
    private float _areaLogTimer = 0f;

    void Start()
    {
        _started = autoStart;
    }

    public void Begin()
    {
        _started = true;
    }

    void Update()
    {
        if (_placed || !_started) return;
        if (planeManager == null || objectToPlace == null) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // 1. Trouver le sol : le plan horizontal le plus BAS
        //    (les tables et plans de travail sont plus hauts).
        ARPlane floor = FindFloor();
        if (floor == null)
        {
            _timer = 0f;   // pas encore de sol : on réarme l'attente
            return;
        }

        // 2. Laisser l'AR se stabiliser avant de poser.
        _timer += Time.deltaTime;
        if (_timer < startDelay) return;

        // 3. Exiger une surface scannée suffisante — uniquement pour les modes qui
        //    ont besoin de place (dispersion sur le plan, recherche d'un coin).
        //    En mode AutourDuJoueur, on pose près de toi : pas besoin d'attendre.
        float area = floor.size.x * floor.size.y;
        if (mode != Mode.AutourDuJoueur && area < minScannedArea)
        {
            _areaLogTimer += Time.deltaTime;
            if (_areaLogTimer > 2f)
            {
                _areaLogTimer = 0f;
                Debug.Log($"[PlaceAroundPlayer] En attente : {area:F1} m² scannés " +
                          $"sur {minScannedArea:F0} m² requis pour {objectToPlace.name}. " +
                          $"Balaye la pièce du regard.");
            }
            return;
        }

        // 3. Choix de la position, au niveau du sol.
        float floorY = floor.transform.position.y;
        Vector3 pos;

        if (mode == Mode.DansUnCoin)
        {
            // Un coin de la pièce = le sommet du contour scanné le plus loin du centre.
            Vector2 corner;
            if (!TryFindCorner(floor, cornerInset, out corner))
            {
                Debug.LogWarning("[PlaceAroundPlayer] Pas de contour exploitable " +
                                 "-> placement au centre du plan.");
                corner = Vector2.zero;
            }

            pos = floor.transform.TransformPoint(new Vector3(corner.x, 0f, corner.y));
            pos.y = floorY;

            Debug.Log($"[PlaceAroundPlayer] Coin trouvé à {corner.magnitude:F1} m du centre " +
                      $"du plan ({floor.boundary.Length} points de contour).");
        }
        else if (mode == Mode.SurToutLePlan)
        {
            // ATTENTION : floor.size est le RECTANGLE englobant du plan, alors que
            // la zone réellement scannée est un polygone irrégulier. On tire donc
            // des points au hasard jusqu'à en trouver un vraiment DANS le polygone.
            Vector2 ext = floor.size * 0.5f;

            Vector2 chosen;
            if (!TryFindPointInside(floor, ext, edgeMargin, out chosen))
            {
                // Aucun point valide trouvé : on se rabat sur le centre du plan.
                Debug.LogWarning("[PlaceAroundPlayer] Zone scannée trop petite/irrégulière " +
                                 "-> placement au centre du plan.");
                chosen = Vector2.zero;
            }

            pos = floor.transform.TransformPoint(new Vector3(chosen.x, 0f, chosen.y));
            pos.y = floorY;

            Debug.Log($"[PlaceAroundPlayer] Plan scanné : {floor.size.x:F1} x {floor.size.y:F1} m " +
                      $"({floor.boundary.Length} points de contour).");
        }
        else
        {
            // Dans un anneau autour du joueur. L'angle part de son regard (0 = devant).
            Vector3 camForward = cam.transform.forward;
            camForward.y = 0f;
            if (camForward.sqrMagnitude < 0.0001f) camForward = Vector3.forward;
            camForward.Normalize();

            // On essaie plusieurs directions/distances jusqu'à tomber sur du sol
            // RÉELLEMENT scanné : sinon l'objet flotte hors du plan et devient
            // impossible à cliquer.
            bool found = false;
            pos = Vector3.zero;

            for (int attempt = 0; attempt < 40; attempt++)
            {
                float angle = randomDirection
                    ? Random.Range(0f, 360f)
                    : fixedAngle;
                float dist = Random.Range(minDistance, maxDistance);
                Vector3 dir = Quaternion.Euler(0f, angle, 0f) * camForward;

                Vector3 candidate = cam.transform.position + dir * dist;
                candidate.y = floorY;

                if (IsOnScannedFloor(floor, candidate))
                {
                    pos = candidate;
                    found = true;
                    break;
                }

                // En direction fixe, inutile d'insister sur le même angle :
                // on rapproche progressivement l'objet du joueur.
                if (!randomDirection)
                {
                    for (float d = maxDistance; d >= 0.5f; d -= 0.25f)
                    {
                        Vector3 c2 = cam.transform.position + dir * d;
                        c2.y = floorY;
                        if (IsOnScannedFloor(floor, c2)) { pos = c2; found = true; break; }
                    }
                    break;
                }
            }

            if (!found)
            {
                Debug.LogWarning($"[PlaceAroundPlayer] Aucun point valide autour du joueur " +
                                 $"pour {objectToPlace.name} -> repli sur le centre du sol.");
                pos = floor.transform.position;
                pos.y = floorY;
            }
        }

        // Sécurité finale, tous modes confondus : si le point n'est pas sur le sol
        // scanné, on se rabat sur le centre du plan (toujours valide).
        if (!IsOnScannedFloor(floor, pos))
        {
            Debug.LogWarning($"[PlaceAroundPlayer] Position hors du sol scanné pour " +
                             $"{objectToPlace.name} -> recentrage sur le plan.");
            pos = floor.transform.position;
            pos.y = floorY;
        }

        objectToPlace.transform.position = pos;

        // Dans un coin : on tourne l'objet vers le centre de la pièce (dos au mur).
        if (mode == Mode.DansUnCoin && faceRoomCenter)
        {
            Vector3 toCenter = floor.transform.position - pos;
            toCenter.y = 0f;
            if (toCenter.sqrMagnitude > 0.0001f)
                objectToPlace.transform.rotation = Quaternion.LookRotation(toCenter);
        }

        objectToPlace.SetActive(true);

        // 4. Caler le bas de l'objet sur le sol, mais une frame plus tard :
        //    les scripts des enfants (ex: la mise à l'échelle du chat) s'exécutent
        //    à l'activation, et il faut leur taille FINALE pour bien caler.
        StartCoroutine(SnapNextFrame(objectToPlace, floorY + heightOffset));

        _placed = true;

        float distToPlayer = Vector3.Distance(
            new Vector3(cam.transform.position.x, 0f, cam.transform.position.z),
            new Vector3(pos.x, 0f, pos.z));

        Debug.Log($"[PlaceAroundPlayer] {objectToPlace.name} posé ({mode}), " +
                  $"à {distToPlayer:F1} m du joueur, sol à {floorY:F2} m.");
    }

    // Cherche un coin de la pièce : le sommet du contour le plus éloigné du centre
    // du plan, puis rentre vers l'intérieur pour être bien posé sur le sol.
    static bool TryFindCorner(ARPlane plane, float inset, out Vector2 result)
    {
        result = Vector2.zero;
        var boundary = plane.boundary;
        if (boundary.Length < 3) return false;

        // Le sommet le plus loin du centre du plan (0,0 en coordonnées locales).
        Vector2 farthest = boundary[0];
        float bestSqr = farthest.sqrMagnitude;
        for (int i = 1; i < boundary.Length; i++)
        {
            float d = boundary[i].sqrMagnitude;
            if (d > bestSqr) { bestSqr = d; farthest = boundary[i]; }
        }

        if (farthest.sqrMagnitude < 0.0001f) return false;

        // On rentre vers le centre jusqu'à être franchement à l'intérieur.
        Vector2 towardCenter = (-farthest).normalized;
        Vector2 p = farthest + towardCenter * inset;

        for (int i = 0; i < 12 && !IsInside(plane, p); i++)
            p += towardCenter * (inset * 0.5f);

        if (!IsInside(plane, p)) return false;

        result = p;
        return true;
    }

    // Tire des points au hasard dans le rectangle du plan jusqu'à en trouver un
    // qui soit réellement à l'intérieur de la zone scannée (le polygone), avec
    // une marge par rapport au bord.
    static bool TryFindPointInside(ARPlane plane, Vector2 ext, float margin, out Vector2 result)
    {
        result = Vector2.zero;
        if (plane.boundary.Length < 3) return false;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            Vector2 p = new Vector2(
                Random.Range(-ext.x, ext.x),
                Random.Range(-ext.y, ext.y));

            // Le point ET son voisinage (marge) doivent être dans la zone scannée,
            // pour ne pas coller au bord de ce qui a été détecté.
            if (IsInside(plane, p) &&
                IsInside(plane, p + new Vector2(margin, 0f)) &&
                IsInside(plane, p + new Vector2(-margin, 0f)) &&
                IsInside(plane, p + new Vector2(0f, margin)) &&
                IsInside(plane, p + new Vector2(0f, -margin)))
            {
                result = p;
                return true;
            }
        }
        return false;
    }

    // Le point (en coordonnées monde) tombe-t-il sur la zone réellement scannée ?
    static bool IsOnScannedFloor(ARPlane plane, Vector3 worldPos)
    {
        if (plane.boundary.Length < 3) return false;

        Vector3 local = plane.transform.InverseTransformPoint(worldPos);
        return IsInside(plane, new Vector2(local.x, local.z));
    }

    // Test "point dans polygone" classique (lancer de rayon) sur le contour du plan.
    static bool IsInside(ARPlane plane, Vector2 point)
    {
        var boundary = plane.boundary;
        int n = boundary.Length;
        if (n < 3) return false;

        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector2 a = boundary[i];
            Vector2 b = boundary[j];

            if (((a.y > point.y) != (b.y > point.y)) &&
                (point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x))
                inside = !inside;
        }
        return inside;
    }

    // Le plan horizontal le plus bas = le sol.
    ARPlane FindFloor()
    {
        ARPlane best = null;
        foreach (ARPlane p in planeManager.trackables)
        {
            if (p.alignment != PlaneAlignment.HorizontalUp) continue;
            if (p.size.x < minPlaneSize || p.size.y < minPlaneSize) continue;

            if (best == null || p.transform.position.y < best.transform.position.y)
                best = p;
        }
        return best;
    }

    // On attend une frame pour que tous les Awake/Start des enfants soient passés
    // (mise à l'échelle, etc.), puis on cale l'objet au sol.
    static IEnumerator SnapNextFrame(GameObject obj, float floorY)
    {
        yield return null;
        SnapBottom(obj, floorY);
    }

    // Décale l'objet pour que le bas de son volume repose sur le sol,
    // quelle que soit la position de son pivot.
    static void SnapBottom(GameObject obj, float floorY)
    {
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        obj.transform.position += new Vector3(0f, floorY - b.min.y, 0f);
    }
}
