using System.Collections;
using UnityEngine;

// Das Bild: startet umgedreht/flach am Boden (wie heruntergefallen).
// Antippen richtet es auf und zeigt die Vorderseite (ein Buch mit Inschrift:
// "Die Schätze verstecken sich oft in den Büchern").
public class FallingPicture : MonoBehaviour
{
    [Header("Vorderseite")]
    public Renderer frontRenderer;     // Renderer, dessen Textur die Vorderseite zeigt
    public Texture bookHintTexture;    // Bild mit dem Hinweistext (Buch)
    public GameObject hintVisual;      // alternativ: Kind-Objekt (Buch-Mesh + Text), das beim Aufrichten sichtbar wird

    [Header("Animation")]
    public float straightenTime = 1f;
    public float readTime = 2f;        // Zeit zum Lesen des Hinweises

    private bool _placed = false;
    private bool _revealed = false;

    // Vom Manager aufgerufen: legt das Bild umgedreht an eine Position.
    public void PlaceBroken(Vector3 pos)
    {
        transform.position = pos;
        // flach hingelegt und um 180° gedreht -> Rückseite nach oben ("gefallen").
        transform.rotation = Quaternion.Euler(90f, 0f, 180f);
        _placed = true;
    }

    // Vom Raetsel3Input aufgerufen, wenn der Spieler das Bild antippt.
    public void OnTapped()
    {
        if (!_placed || _revealed) return;
        _revealed = true;
        StartCoroutine(Straighten());
    }

    IEnumerator Straighten()
    {
        // Zeigt die Vorderseite mit dem Buch-Hinweis.
        if (frontRenderer != null && bookHintTexture != null)
            frontRenderer.material.mainTexture = bookHintTexture;
        if (hintVisual != null)
            hintVisual.SetActive(true);

        Quaternion from = transform.rotation;

        // Aufrecht, zur Kamera gedreht.
        Vector3 toCam = Camera.main.transform.position - transform.position;
        toCam.y = 0f;
        Quaternion to = (toCam.sqrMagnitude > 0.0001f)
            ? Quaternion.LookRotation(toCam)
            : transform.rotation;

        float t = 0f;
        while (t < straightenTime)
        {
            t += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(from, to, t / straightenTime);
            yield return null;
        }
        transform.rotation = to;

        yield return new WaitForSeconds(readTime);

        if (Raetsel3Manager.Instance != null)
            Raetsel3Manager.Instance.OnPictureRevealed();
    }
}
