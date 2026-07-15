using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class InputManager : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager arRaycastManager;

    private PuzzlePiece _activePiece;

    void Update()
    {
#if UNITY_EDITOR
        // EDITOR — Maus
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Getroffen: " + hit.collider.gameObject.name);

                PhoneRinging phone = hit.collider.GetComponent<PhoneRinging>();
                if (phone != null) { phone.OnPhoneTapped(); return; }

                ImageReveal img = hit.collider.GetComponent<ImageReveal>();
                if (img != null)
                {
                    GameManager.Instance.OnPuzzleTapped();
                    return;
                }

                PuzzlePiece piece = hit.collider.GetComponent<PuzzlePiece>();
                if (piece != null)
                {
                    _activePiece = piece;
                    _activePiece.OnStartDrag(Input.mousePosition);
                }
            }
        }

        if (Input.GetMouseButton(0) && _activePiece != null)
            _activePiece.OnDrag(Input.mousePosition);

        if (Input.GetMouseButtonUp(0) && _activePiece != null)
        {
            _activePiece.OnEndDrag();
            _activePiece = null;
        }
#endif

#if !UNITY_EDITOR
        // ANDROID — Touch
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);
        Ray ray = Camera.main.ScreenPointToRay(touch.position);

        if (touch.phase == TouchPhase.Began)
        {
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                PhoneRinging phone = hit.collider.GetComponent<PhoneRinging>();
                if (phone != null) { phone.OnPhoneTapped(); return; }

                ImageReveal img = hit.collider.GetComponent<ImageReveal>();
                if (img != null)
                {
                    GameManager.Instance.OnPuzzleTapped();
                    return;
                }

                PuzzlePiece piece = hit.collider.GetComponent<PuzzlePiece>();
                if (piece != null)
                {
                    _activePiece = piece;
                    _activePiece.OnStartDrag(touch.position);
                }
            }
        }

        if (touch.phase == TouchPhase.Moved && _activePiece != null)
            _activePiece.OnDrag(touch.position);

        if (touch.phase == TouchPhase.Ended && _activePiece != null)
        {
            _activePiece.OnEndDrag();
            _activePiece = null;
        }
#endif
    }
}
