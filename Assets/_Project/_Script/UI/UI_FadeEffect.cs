using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UI_FadeEffect : MonoBehaviour
{

    [SerializeField] private bool startActive = false;
    [SerializeField] private AnimationCurve _fadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve _fadeOutCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private bool _isShowing = false;
    private bool _isFading = false;
    public bool IsShowing => _isShowing;
    public bool IsFading => _isFading;
    
    private CanvasGroup canvas = null;
    private Coroutine _effectCoroutine = null;

    public void Awake()
    {
        canvas = GetComponent<CanvasGroup>();

        gameObject.SetActive(startActive);
        if (startActive)
            ShowForced();
        else
            HideForced();
    }

    public void FadeIn(Action callback = null)
    {
        if (!_isShowing && !_isFading)
            Effect(true, callback);
    }
    
    public void FadeInMainMenu(Action callback = null)
    {
        Effect(true, callback);
    }

    public void ShowForced()
    {
        _isShowing = true;
        _isFading = false;

        gameObject.SetActive(true);
        canvas.interactable = true;
        canvas.alpha = _fadeInCurve.Evaluate(_fadeInCurve.keys[_fadeInCurve.length - 1].time);
    }

    public void FadeOut(Action callback = null)
    {
        if (_isShowing && !_isFading)
            Effect(false, callback);
    }

    public void HideForced()
    {
        _isShowing = false;
        _isFading = false;

        gameObject.SetActive(true);
        canvas.alpha = _fadeOutCurve.Evaluate(_fadeOutCurve.keys[0].time);
        canvas.interactable = false;
        gameObject.SetActive(false);
    }

    private void Effect(bool fadeIn = true, Action callback = null)
    {
        if (!_isFading)
        {
            if (_effectCoroutine != null) 
                MonoBehaviorHelper.StopCoroutine(_effectCoroutine);
            
            _effectCoroutine = MonoBehaviorHelper.StartCoroutine(EffectCoroutine(fadeIn, callback));
        }
    }

    private IEnumerator EffectCoroutine(bool fadeIn = true, Action callback = null)
    {
        while(!gameObject.activeSelf)
            gameObject.SetActive(true);

        _isFading = true;
        canvas.interactable = true;
        
        float startAnimationTime = fadeIn ? _fadeInCurve.keys[0].time : _fadeOutCurve.keys[0].time;
        float endAnimationTime = 
            fadeIn ?
                _fadeInCurve.keys[_fadeInCurve.length - 1].time :
                _fadeOutCurve.keys[_fadeOutCurve.length - 1].time;

        float currentTime = startAnimationTime;

        while(currentTime < endAnimationTime)
        {
            currentTime += Time.deltaTime;
            float alpha = fadeIn ? _fadeInCurve.Evaluate(currentTime) : _fadeOutCurve.Evaluate(currentTime);
            canvas.alpha = fadeIn ? alpha : _fadeOutCurve.Evaluate(endAnimationTime) - alpha;

            yield return null;
        }
        
        if (!fadeIn)
        {
            canvas.interactable = false;
            gameObject.SetActive(false);
        }

        _isShowing = fadeIn;
        _isFading = false;
        callback?.Invoke();
        canvas.alpha = fadeIn ? _fadeInCurve.Evaluate(endAnimationTime) : _fadeOutCurve.Evaluate(startAnimationTime);

        yield return null;
    }

}
