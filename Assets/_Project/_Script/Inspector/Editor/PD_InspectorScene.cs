using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(InspectorScene))]
public class PD_InspectorScene : PropertyDrawer
{

    private const string sceneVariableName = "_scene";
    private const string visibleLabelVariableName = "_visibleLabel";
    private const string emptyValue = "Null Scene";

    private readonly Color missingEnumColor = new(1, 0, 0, 0.2f);

    public override bool CanCacheInspectorGUI(SerializedProperty property)
    {
        return false;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var sceneProperty = property.FindPropertyRelative(sceneVariableName);

        var visibleLabelProperty = property.FindPropertyRelative(visibleLabelVariableName);

        var rectButton = position;

        if (visibleLabelProperty.boolValue)
            rectButton = EditorGUI.PrefixLabel(position, label);

        var name = sceneProperty.stringValue;

        var tooltip = "Select a Scene";

        bool hasName = !name.IsEmpty();

        if (name.IsEmpty())
            name = emptyValue;
        else
        {
            tooltip = name.Replace("/", " > ");

            var paths = name.Split("/");
            name = "";

            for (int i = Mathf.Max(paths.Length - 1, 0); i < paths.Length; i++)
                name += $"{paths[i]}";
        }

        if (GUI.Button(rectButton, new GUIContent(name, tooltip)))
            DrawMenu(property, sceneProperty);

        if (!hasName) EditorGUI.DrawRect(position, missingEnumColor);

        property.serializedObject.ApplyModifiedProperties();

#if !UNITY_EDITOR
        sceneProperty.Dispose();

        property.Dispose();
#endif
    }

    private void DrawMenu(SerializedProperty property, SerializedProperty relativeproperty)
    {
        GenericMenu menu = new();

        menu.AddItem(new GUIContent(emptyValue), false, () =>
        {
            relativeproperty.stringValue = "";

            EditorUtility.SetDirty(property.serializedObject.targetObject);

            property.serializedObject.ApplyModifiedProperties();
        });

        foreach (var item in InspectorScene.Extensions.Scenes)
        {
            menu.AddItem(new GUIContent(item.Key), false, () =>
            {
                relativeproperty.stringValue = item.Key;

                EditorUtility.SetDirty(property.serializedObject.targetObject);

                property.serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }
}
