using System.Collections;
using UnityEngine;

// Das Buch: erscheint auf dem Boden. Antippen öffnet es und zeigt die Ziffer.
public class PuzzleBook : MonoBehaviour
{
    [Header("Öffnen-Animation (optional)")]
    public Transform cover;            // beweglicher Buchdeckel (klappt beim Öffnen auf)
    public float openAngle = 160f;
    public float openTime = 0.6f;

    [Header("Darstellung")]
    public GameObject closedVisual;    // geschlossenes Buch
    public GameObject openVisual;      // offenes Buch (mit sichtbarer Ziffer)

    private bool _opened = false;

    // Vom Manager aufgerufen: platziert das Buch, zur Kamera ausgerichtet.
    public void PlaceAt(Vector3 pos)
    {
        transform.position = pos;

        Vector3 toCam = Camera.main.transform.position - pos;
        toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam);

        if (closedVisual != null) closedVisual.SetActive(true);
        if (openVisual != null) openVisual.SetActive(false);
        _opened = false;
    }

    // Vom Raetsel3Input aufgerufen, wenn der Spieler das Buch antippt.
    public void OnTapped()
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        // Einfache Öffnen-Animation: Deckel klappt auf (falls zugewiesen).
        if (cover != null)
        {
            Quaternion from = cover.localRotation;
            Quaternion to = from * Quaternion.Euler(0f, 0f, -openAngle);
            float t = 0f;
            while (t < openTime)
            {
                t += Time.deltaTime;
                cover.localRotation = Quaternion.Slerp(from, to, t / openTime);
                yield return null;
            }
            cover.localRotation = to;
        }

        // Wechselt auf die offene Darstellung mit der Ziffer.
        if (closedVisual != null) closedVisual.SetActive(false);
        if (openVisual != null) openVisual.SetActive(true);

        yield return new WaitForSeconds(0.3f);

        if (Raetsel3Manager.Instance != null)
            Raetsel3Manager.Instance.OnBookOpened();
    }
}
