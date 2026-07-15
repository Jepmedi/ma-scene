using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;

public class PhoneRinging : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource ringingAudioSource;   // Klingelton in Schleife
    public AudioSource voiceAudioSource;     // Sprachnachricht "dreh dich um..."

    [Header("Einstellungen")]
    public float ringVolume = 1f;
    public GameObject tapHintUI;             // UI "Tippe auf das Telefon" (optional)

    private bool _answered = false;

    void Start()
    {
        // Die POSITION wird jetzt von PhoneARPlacer verwaltet (Platzierung auf einer AR-Ebene).
        // Hier wird nur das Klingeln gestartet, sobald das Objekt aktiv wird.
        StartRinging();
    }

    void StartRinging()
    {
        ringingAudioSource.loop = true;
        ringingAudioSource.spatialBlend = 1f;  // räumlicher 3D-Klang
        ringingAudioSource.volume = ringVolume;
        ringingAudioSource.Play();

        if (tapHintUI != null)
            tapHintUI.SetActive(true);
    }

    // Wird vom InputManager aufgerufen, wenn der Nutzer auf das Telefon tippt
    public void OnPhoneTapped()
    {
        if (_answered) return;
        _answered = true;

        ringingAudioSource.Stop();

        if (tapHintUI != null)
            tapHintUI.SetActive(false);

        StartCoroutine(PlayVoiceMessage());
    }

    IEnumerator PlayVoiceMessage()
    {
        voiceAudioSource.Play();
        // Wir warten das Ende der Nachricht ab, bevor das Puzzle ausgelöst wird
        yield return new WaitForSeconds(voiceAudioSource.clip.length);

        // Löst den nächsten Schritt aus
        GameManager.Instance.OnPhoneAnswered();
    }
}
