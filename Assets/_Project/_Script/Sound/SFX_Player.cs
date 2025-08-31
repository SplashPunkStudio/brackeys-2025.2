using UnityEngine;

public class SFX_Player : Singleton<SFX_Player>
{

    [SerializeField] private SO_Sound _sfxSelection;
    [SerializeField] private SO_Sound _sfxClick;
    [SerializeField] private SO_Sound _sfxMemoryCollect;
    [SerializeField] private SO_Sound _sfxTransitionScene;

    protected override void Init()
    {
        base.Init();

        DontDestroyOnLoad(gameObject);
    }

    private void OnPlaySfxSelection() => _sfxSelection.Play();
    private void OnPlaySfxClick() => _sfxClick.Play();
    private void OnPlaySfxMemoryCollect() => _sfxMemoryCollect.Play();
    private void OnPlaySfxTransitionScene() => _sfxTransitionScene.Play();

    void OnEnable()
    {
        Manager_Events.Sound.SFX.OnSfxPress += OnPlaySfxClick;
        Manager_Events.Sound.SFX.PlaySfxMemoryCollect += OnPlaySfxMemoryCollect;
        Manager_Events.Sound.SFX.PlaySfxSelection += OnPlaySfxSelection;
        Manager_Events.Sound.SFX.PlaySfxTransitionScene += OnPlaySfxTransitionScene;
    }

    void OnDisable()
    {
        Manager_Events.Sound.SFX.PlaySfxSelection -= OnPlaySfxSelection;
        Manager_Events.Sound.SFX.PlaySfxMemoryCollect -= OnPlaySfxMemoryCollect;
        Manager_Events.Sound.SFX.PlaySfxSelection -= OnPlaySfxSelection;
        Manager_Events.Sound.SFX.PlaySfxTransitionScene -= OnPlaySfxTransitionScene;
    }

}
