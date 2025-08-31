using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFX_Sound : MonoBehaviour
{

    [SerializeField] private AudioSource _audioSource;

    private SO_Sound m_soSound = null;
    public SO_Sound Sound => m_soSound;

    public void Setup(SO_Sound sound)
    {
        m_soSound = sound;

        var clip = sound.clip[Random.Range(0, sound.clip.Length)];

        transform.name = $"[{sound.type}_Sound] - {clip.name}";

        _audioSource.outputAudioMixerGroup = sound.group;

        _audioSource.loop = sound.loop;

        _audioSource.spatialBlend = sound.spatialBlend ? 1f : 0f;

        _audioSource.clip = clip;

        _audioSource.pitch = sound.pitch;

        _audioSource.volume = Mathf.Clamp01(sound.volume);

        _audioSource.Play();

        if (!sound.loop)
            MonoBehaviorHelper.StartCoroutine(WaitSound());
    }

    public void StopSound()
    {
        _audioSource.Stop();

        m_soSound = null;
    }

    private IEnumerator WaitSound()
    {
        while(_audioSource.isPlaying)
            yield return null;

        Manager_Events.Sound.OnReleaseSound.Notify(this);

        m_soSound = null;
    }

}
