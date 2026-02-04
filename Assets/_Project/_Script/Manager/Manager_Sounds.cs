using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class Manager_Sounds : Singleton<Manager_Sounds>
{

    [SerializeField] private SO_Sound _pressSfx;

    [SerializeField] private SFX_Sound _prefabSfxSound;

    private readonly List<SFX_Sound> m_ambientSounds = new();
    private readonly List<SFX_Sound> m_musicSounds = new();
    private readonly List<SFX_Sound> m_sfxSounds = new();

    private ObjectPool<SFX_Sound> m_poolSfxSounds;

    private Dictionary<SO_Sound, SFX_Sound> m_dictSounds = new();

    protected override void Init()
    {
        base.Init();

        DontDestroyOnLoad(gameObject);

        m_poolSfxSounds = new(
            () => Instantiate(_prefabSfxSound, transform),
            (obj) => obj.gameObject.SetActive(true),
            (obj) => obj.gameObject.SetActive(false),
            (obj) => Destroy(obj.gameObject),
            true,
            2,
            50
        );
    }

    private void OnPlay(SO_Sound so_sound)
    {
        var sfx = m_poolSfxSounds.Get();

        switch (so_sound.type)
        {
            case SO_SoundType.MUSIC: OnReleaseByType(SO_SoundType.MUSIC); m_musicSounds.Add(sfx); break;
            case SO_SoundType.AMBIENT: m_ambientSounds.Add(sfx); break;
            case SO_SoundType.SFX: m_sfxSounds.Add(sfx); break;
            default: break;
        }

        sfx.Setup(so_sound);

        if (!m_dictSounds.ContainsKey(so_sound))
            m_dictSounds.Add(so_sound, sfx);
    }

    private void OnStop(SO_Sound so_sound)
    {
        if (m_dictSounds.ContainsKey(so_sound))
        {
            var sfx = m_dictSounds[so_sound];

            OnReleaseSfx(sfx);

            sfx.StopSound();
        }
    }

    private void OnReleaseByType(SO_SoundType type)
    {
        List<SFX_Sound> list = type switch
        {
            SO_SoundType.AMBIENT => m_ambientSounds,
            SO_SoundType.SFX => m_sfxSounds,
            _ => m_musicSounds,
        };

        var listToRelease = new List<SFX_Sound>();

        foreach (var sfxSound in list)
        {
            if (sfxSound.Sound == null || sfxSound.Sound.type != type)
                continue;

            listToRelease.Add(sfxSound);
        }

        foreach (var sfxSound in listToRelease)
            sfxSound.StopSound();
    }

    private void OnReleaseSfx(SFX_Sound sfxSound)
    {
        if (m_dictSounds.ContainsKey(sfxSound.Sound))
            m_dictSounds.Remove(sfxSound.Sound);

        m_poolSfxSounds.Release(sfxSound);
    }

    private void OnSfxPress()
    {
        OnPlay(_pressSfx);
    }

    private void OnVolume(SO_Sound so_sound, float volume)
    {
        if (!m_dictSounds.ContainsKey(so_sound))
            return;

        m_dictSounds[so_sound].SetVolume(volume);
    }


    void OnEnable()
    {
        Manager_Events.Sound.OnPlay += OnPlay;
        Manager_Events.Sound.OnStop += OnStop;
        Manager_Events.Sound.OnVolume += OnVolume;
        Manager_Events.Sound.OnReleaseSound += OnReleaseSfx;
        Manager_Events.Sound.OnReleaseByType += OnReleaseByType;

        Manager_Events.Sound.SFX.OnSfxPress += OnSfxPress;
    }

    void OnDisable()
    {
        Manager_Events.Sound.OnPlay -= OnPlay;
        Manager_Events.Sound.OnStop -= OnStop;
        Manager_Events.Sound.OnVolume -= OnVolume;
        Manager_Events.Sound.OnReleaseSound -= OnReleaseSfx;
        Manager_Events.Sound.OnReleaseByType -= OnReleaseByType;

        Manager_Events.Sound.SFX.OnSfxPress -= OnSfxPress;
    }


}
