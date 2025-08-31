using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class InspectorScene
{

    [SerializeField] private bool _visibleLabel = true;
    [SerializeField] private string _scene;

    public string Scene => _scene;

    private int BuildIndex => SceneUtility.GetBuildIndexByScenePath(Extensions.Scenes[_scene]);

    private bool ValidScene => !_scene.IsEmpty() && Extensions.Scenes.ContainsKey(_scene);

    public InspectorScene() { }

    public InspectorScene(string scene)
    {
        _scene = scene;
    }

    public AsyncOperation LoadSceneAsync(LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!ValidScene)
            return null;

        return SceneManager.LoadSceneAsync(BuildIndex, mode);
    }

    public void LoadScene(LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (!ValidScene)
            return;

        SceneManager.LoadScene(BuildIndex, mode);
    }

    public AsyncOperation UnloadSceneAsync()
    {
        if (!ValidScene)
            return null;

        return SceneManager.UnloadSceneAsync(BuildIndex);
    }

    public override string ToString()
    {
        return $"{_scene}";
    }

    public static implicit operator InspectorScene(string value) => new(value);

    public class Extensions
    {

        public static Dictionary<string, string> Scenes
        {
            get
            {
                Dictionary<string, string> scenes = new();

                for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var path = SceneUtility.GetScenePathByBuildIndex(i);

                    if (path.IsEmpty())
                        continue;

                    scenes.Add(StringUtils.GetReducedPath(path, ".unity", 2), path);
                }

                return scenes;
            }
        }

    }

}
