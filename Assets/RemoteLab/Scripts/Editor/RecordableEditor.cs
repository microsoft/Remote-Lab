using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Recordable))]
public class RecordableEditor : Editor
{
    private System.Guid generatedGuid;

    private void OnEnable()
    {
        var guidProperty = serializedObject.FindProperty("guidString");
        var instantiatedProp = serializedObject.FindProperty("isInstantiatedAtRuntime");

        if (!guidProperty.ToString().Equals("") || instantiatedProp.boolValue)
            return;

        generatedGuid = System.Guid.NewGuid();
        guidProperty.stringValue = generatedGuid.ToString();
        serializedObject.ApplyModifiedProperties();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Recordable script = (Recordable)target;

        script.isInstantiatedAtRuntime = EditorGUILayout.Toggle("Is Instantiated At Runtime", script.isInstantiatedAtRuntime);

        var guidProperty = serializedObject.FindProperty("guidString");
        var instantiatedProp = serializedObject.FindProperty("isInstantiatedAtRuntime");
        EditorGUILayout.PropertyField(guidProperty);

        if (guidProperty.stringValue.Equals("") && !instantiatedProp.boolValue)
        {
            generatedGuid = System.Guid.NewGuid();
            guidProperty.stringValue = generatedGuid.ToString();
        }

        var resourceProperty = serializedObject.FindProperty("resourceName");

        if (script.isInstantiatedAtRuntime)
        {
            resourceProperty.stringValue = EditorGUILayout.TextField("Resource Path", script.resourceName);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
