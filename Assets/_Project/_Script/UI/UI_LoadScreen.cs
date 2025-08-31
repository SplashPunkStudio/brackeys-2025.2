using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class UI_LoadScreen : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new(0.5f);
    private static string[] m_phrases = new[]
       {
            "Cyclops sweat crystallizes ice, creating sudden holes on the curling sheet.",
            "Giant squid tentacle-brooms leave suction marks, trapping stones.",
            "Dragon scales on the broom scratch the ice, altering stone trajectory.",
            "Invisible monsters \"adjust\" stones but sometimes step on ice, cracking it.",
            "Harpy sonic screams can vibrate ice, moving stones on the Button.",
            "Slime monsters sweeping leave a sticky trail, slowing down stones.",
            "Multi-headed Hydras argue sweeping, causing coordinated chaos.",
            "Olfactory referees detect \"fear,\" deducting points for excessive odor.",
            "Trolls regenerate broken stones, but they might grow back on the ice.",
            "Luminous monsters glow too much, blinding opponents and their own sweepers."
        };

    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtSubtitle;
    [SerializeField] private Slider _slider;

    private static InspectorScene m_nextScene = null;
    private static string m_title = null;
    private static string m_subtitle = null;
    public static void SetInfos(InspectorScene scene)
    {
        m_nextScene = scene;
        m_title = "Ice Scream Arena";
        m_subtitle = m_phrases[Random.Range(0, m_phrases.Length)];
    }

    void Start()
    {
        _txtTitle.SetText(m_title);
        _txtSubtitle.SetText(m_subtitle);

        _slider.value = 0f;

        StartCoroutine(SimulateLoadingProcess());
    }

    private IEnumerator SimulateLoadingProcess()
    {
        float currentProgress = 0f;
        float targetProgress = 0.3f;

        for (int i = 0; i < 4; i++)
            yield return _waitForSeconds0_5;

        yield return StartCoroutine(UpdateSliderProgress(currentProgress, targetProgress, 0.5f));
        currentProgress = targetProgress;

        for (int i = 0; i < 2; i++)
            yield return _waitForSeconds0_5;

        targetProgress = 0.6f;
        yield return StartCoroutine(UpdateSliderProgress(currentProgress, targetProgress, 0.5f));
        currentProgress = targetProgress;

        for (int i = 0; i < 3; i++)
            yield return _waitForSeconds0_5;

        targetProgress = 0.9f;
        yield return StartCoroutine(UpdateSliderProgress(currentProgress, targetProgress, 1.0f));
        currentProgress = targetProgress;

        for (int i = 0; i < 2; i++)
            yield return _waitForSeconds0_5;

        targetProgress = 1.0f;
        yield return StartCoroutine(UpdateSliderProgress(currentProgress, targetProgress, 0.25f));

        for (int i = 0; i < 3; i++)
            yield return _waitForSeconds0_5;

        Manager_Events.Scene.Transition.Notify(m_nextScene);
    }

    private IEnumerator UpdateSliderProgress(float startValue, float endValue, float simulatedWorkDuration)
    {
        float startTime = Time.time;
        float elapsedTime = 0f;

        while (elapsedTime < simulatedWorkDuration)
        {
            elapsedTime = Time.time - startTime;
            float t = elapsedTime / simulatedWorkDuration;

            _slider.value = Mathf.Lerp(startValue, endValue, t);

            yield return null;
        }

        _slider.value = endValue;
    }
}