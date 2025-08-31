using System.Collections;
using UnityEngine;

public class Manager_Transition : Singleton<Manager_Transition>
{

    [SerializeField] private InspectorScene _loadingScene;

    [SerializeField] private CanvasGroup _canvas;
    [SerializeField] private Animator _animator;
    [SerializeField] private float _minTransitionDelay;
    [SerializeField] private SO_Sound _sfxTransition;

    private bool m_transition = false;
    private bool m_show = false;

    protected override void Init()
    {
        base.Init();

        _canvas.interactable = false;
        _canvas.blocksRaycasts = false;

        DontDestroyOnLoad(gameObject);
    }

    private void Transition(InspectorScene scene)
    {
        MonoBehaviorHelper.StartCoroutine(LoadScene(scene));
    }

    private void TransitionWithLoading(InspectorScene nextScene)
    {
        Transition(_loadingScene);

        UI_LoadScreen.SetInfos(nextScene);
    }

    private void TransitionNoDelay(InspectorScene scene)
    {
        m_transition = false;
        m_show = false;

        _canvas.interactable = false;
        _canvas.blocksRaycasts = false;

        scene.LoadScene();
    }

    private void Show()
    {
        if (m_transition)
            return;

        Manager_Events.Sound.OnPlay.Notify(_sfxTransition);

        _canvas.interactable = true;
        _canvas.blocksRaycasts = true;

        m_transition = true;
        m_show = false;

        _animator.Play("ScaleIn");
    }

    private void Hide()
    {
        if (m_transition)
            return;

        m_transition = true;
        m_show = true;

        _animator.Play("ScaleOut");
    }

    private void EndAnimation()
    {
        m_transition = false;
        m_show = !m_show;

        if (!m_show)
        {
            Manager_Events.Sound.OnStop.Notify(_sfxTransition);

            _canvas.interactable = false;
            _canvas.blocksRaycasts = false;
            _animator.Play("Idle");
        }
    }

    private IEnumerator LoadScene(InspectorScene scene)
    {
        var asyncScene = scene.LoadSceneAsync();
        asyncScene.allowSceneActivation = false;

        float time = Time.time;

        Show();

        while (asyncScene.progress < .9f)
            yield return null;

        while (m_transition)
            yield return null;

        asyncScene.allowSceneActivation = true;

        while (time + _minTransitionDelay >= Time.time)
            yield return null;

        while (asyncScene.progress < 1f)
            yield return null;

        Hide();
    }

    void OnEnable()
    {
        Manager_Events.Scene.Transition += Transition;
        Manager_Events.Scene.TransitionWithLoading += TransitionWithLoading;
        Manager_Events.Scene.TransitionNoDelay += TransitionNoDelay;
    }

    void OnDisable()
    {
        Manager_Events.Scene.Transition -= Transition;
        Manager_Events.Scene.TransitionWithLoading -= TransitionWithLoading;
        Manager_Events.Scene.TransitionNoDelay -= TransitionNoDelay;
    }

}
