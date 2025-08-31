using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Video;

public class UI_SplashScreen : MonoBehaviour
{

    [SerializeField] private InspectorScene _nextScene;

    [SerializeField] private float _logoDuration;
    [SerializeField] private VideoPlayer _videoPlayer;

    void Start()
    {
        _videoPlayer.prepareCompleted += OnPrepareCompletedJmj;

        _videoPlayer.loopPointReached += OnLoopPointReachedJmj;

        _videoPlayer.Prepare();
    }

    private void OnPrepareCompletedJmj(VideoPlayer source)
    {
        _videoPlayer.Play();
    }

    private void OnLoopPointReachedJmj(VideoPlayer source)
    {
        WaitVideoDelay();
    }

    private async void WaitVideoDelay()
    {
        await UniTask.Delay((int)(_logoDuration * 1000));

        Manager_Events.Scene.TransitionNoDelay.Notify(_nextScene);
    }

}
