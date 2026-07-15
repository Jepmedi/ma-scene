using UnityEngine;
using System.Collections;

public class ImageReveal : MonoBehaviour
{
    [Header("Bild")]
    public GameObject imageQuad;
    public float displayDuration = 4f;

    [Header("Zerstörungs-Effekt")]
    public ParticleSystem destroyParticles;

    private Renderer _renderer;

    void Awake()
    {
        _renderer = imageQuad.GetComponent<Renderer>();
        MakeMaterialTransparent();
    }

    // Konfiguriert das URP/Lit-Material so, dass die Alpha-Änderung SICHTBAR ist.
    // Ohne das ignoriert ein undurchsichtiges Material das Alpha und das Bild verschwindet nie.
    void MakeMaterialTransparent()
    {
        if (_renderer == null) return;
        Material m = _renderer.material; // Instanz
        m.SetFloat("_Surface", 1f);                 // 0 = Opaque, 1 = Transparent
        m.SetFloat("_Blend", 0f);                   // 0 = Alpha
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    public void StartReveal()
    {
        StartCoroutine(RevealRoutine());
    }

    IEnumerator RevealRoutine()
    {
        Debug.Log("RevealRoutine gestartet!");

        yield return StartCoroutine(FadeIn());
        Debug.Log("FadeIn beendet!");

        yield return new WaitForSeconds(displayDuration);
        Debug.Log("Wartezeit beendet!");

        if (destroyParticles != null)
        {
            destroyParticles.transform.position = imageQuad.transform.position;
            destroyParticles.Play();
        }

        yield return StartCoroutine(FadeOut());
        Debug.Log("FadeOut beendet!");

        // Stellt sicher, dass das Bild verschwindet, auch wenn der Fade keine sichtbare Wirkung hatte.
        imageQuad.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnImageDestroyed();
        else
            Debug.LogError("[ImageReveal] GameManager.Instance ist null!");
    }

    IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float duration = 0.5f;
        Color color = _renderer.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            _renderer.material.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        _renderer.material.color = new Color(color.r, color.g, color.b, 1f);
    }

    IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float duration = 1f;
        Color color = _renderer.material.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            _renderer.material.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        _renderer.material.color = new Color(color.r, color.g, color.b, 0f);
    }
}
