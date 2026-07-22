using UnityEngine;

// Leitet die Taps (Editor-Maus / Android-Touch) an das Bild bzw. das Buch weiter.
public class Raetsel3Input : MonoBehaviour
{
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
        if (!tapped) return;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // GetComponentInParent: der Collider kann auf einem Kind-Objekt liegen.
            FallingPicture pic = hit.collider.GetComponentInParent<FallingPicture>();
            if (pic != null) { pic.OnTapped(); return; }

            PuzzleBook book = hit.collider.GetComponentInParent<PuzzleBook>();
            if (book != null) { book.OnTapped(); return; }
        }
    }
}
