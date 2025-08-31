using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InspectorEvent))]
public class PD_InspectorEvent : PropertyDrawer
{

    private const string eventVariableName = "_event";
    private const string orderVariableName = "_order";
    private const string emptyValue = "Null Event";

    private readonly Color missingEnumColor = new(1, 0, 0, 0.2f);

    public override bool CanCacheInspectorGUI(SerializedProperty property)
    {
        return false;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var drawFromInspector = property.serializedObject.targetObject is MonoBehaviour;

        var eventRelativeProperty = property.FindPropertyRelative(eventVariableName);

        var orderRelativeProperty = property.FindPropertyRelative(orderVariableName);

        var rectButton = position;

        if (drawFromInspector)
            rectButton = EditorGUI.PrefixLabel(position, label);

        var eventName = eventRelativeProperty.stringValue;

        var tooltip = "Select an Event";

        bool hasName = !eventName.IsEmpty();

        if (eventName.IsEmpty())
            eventName = emptyValue;
        else
        {
            tooltip = eventName.Replace("/", " > ");

            var paths = eventName.Split("/");

            eventName = "";

            for (int i = Mathf.Max(paths.Length - 1, 0); i < paths.Length; i++)
                eventName += $"{paths[i]}";
        }

        if (hasName)
            rectButton = DrawOrderField(rectButton, orderRelativeProperty);

        if (GUI.Button(rectButton, new GUIContent(eventName, tooltip)))
            DrawMenu(property, eventRelativeProperty);

        if (!hasName) EditorGUI.DrawRect(position, missingEnumColor);

        property.serializedObject.ApplyModifiedProperties();

#if !UNITY_EDITOR
        eventRelativeProperty.Dispose();

        orderRelativeProperty.Dispose();

        property.Dispose();
#endif
    }

    private Rect DrawOrderField(Rect rect, SerializedProperty property)
    {
        var rectOrder = rect;

        rectOrder.width = 40;

        property.intValue = EditorGUI.IntField(rectOrder, property.intValue);

        var space = rectOrder.width + 3;

        rect.x += rectOrder.width + 3;

        rect.width -= space;

        return rect;
    }

    private void DrawMenu(SerializedProperty property, SerializedProperty relativeProperty)
    {
        GenericMenu menu = new();

        menu.AddItem(new GUIContent(emptyValue), false, () =>
        {
            relativeProperty.stringValue = "";

            property.FindPropertyRelative(orderVariableName).intValue = 0;

            EditorUtility.SetDirty(property.serializedObject.targetObject);

            property.serializedObject.ApplyModifiedProperties();
        });

        foreach (var item in Manager_Events.Extensions.Fields)
        {
            menu.AddItem(new GUIContent(item.Key), false, () =>
            {
                relativeProperty.stringValue = item.Key;

                EditorUtility.SetDirty(property.serializedObject.targetObject);

                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

}
