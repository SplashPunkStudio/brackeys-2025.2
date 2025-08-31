using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.EventTrigger;

public static class ExtensionMethods
{

    private const string ResourcesFolder = "Assets/Resources";

    public static bool IsEmpty(this string value) => value == null || value == "" || value.Length <= 0 || value.ToLower() == "null";

    public static bool IsEmpty<T>(this T[] value) => value == null || value.Length <= 0;

    public static bool IsEmpty<T>(this IList<T> value) => value == null || value.Count <= 0;

    public static bool IsEmpty<T, U>(this Dictionary<T, U> value) => value == null || value.Count <= 0;

    public static bool CompareTag(this Component self, Tags tag) => self.CompareTag(tag.ToString());

    public static string ToSingleString(this string[] values) => values.ToList().ToSingleString();

    public static void SetLayerRecursively(this GameObject self, LayerMask layer)
    {
        self.layer = layer;

        foreach (Transform child in self.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    public static void SetLayerRecursively(this GameObject self, string layer)
    {
        self.SetLayerRecursively(LayerMask.NameToLayer(layer));
    }

    public static string ToSingleString(this List<string> values)
    {
        string result = "";

        foreach (var value in values)
            result += $"{value}{(value == values[^1] ? "." : ", ")}";

        return result;
    }

    public static float Duration(this AnimationCurve self)
    {
        if (self.length > 0)
            return self[self.length - 1].time;

        return 0f;
    }

    public static void Shuffle<T>(this IList<T> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
    }

    public static void Setup(this Button button, UnityAction @event, List<Entry> triggers = null)
    {
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(@event);

        if (!triggers.IsEmpty())
        {
            if (!button.TryGetComponent(out EventTrigger eventTrigger))
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();

            eventTrigger.triggers.Clear();
            eventTrigger.triggers.AddRange(triggers);
        }
    }

    public static void Setup(this Slider slider, UnityAction<float> @event, List<Entry> triggers = null)
    {
        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(@event);

        if (!triggers.IsEmpty())
        {
            if (!slider.TryGetComponent(out EventTrigger eventTrigger))
                eventTrigger = slider.gameObject.AddComponent<EventTrigger>();

            eventTrigger.triggers.Clear();
            eventTrigger.triggers.AddRange(triggers);
        }
    }

    public static void Setup(this TMP_Dropdown dropdown, UnityAction<int> @event, List<Entry> triggers = null)
    {
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener(@event);

        if (!triggers.IsEmpty())
        {
            if (!dropdown.TryGetComponent(out EventTrigger eventTrigger))
                eventTrigger = dropdown.gameObject.AddComponent<EventTrigger>();

            eventTrigger.triggers.Clear();
            eventTrigger.triggers.AddRange(triggers);
        }
    }

}
