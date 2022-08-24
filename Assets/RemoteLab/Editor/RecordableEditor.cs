using UnityEngine;
using UnityEditor;

namespace RemoteLab.Editor
{
    [CustomEditor(typeof(Recordable))]
    public class RecordableEditor : UnityEditor.Editor
    {
        private System.Guid generatedGuid;

        private void OnEnable()
        {
            var guidProperty = serializedObject.FindProperty("guidString");

            if (!guidProperty.ToString().Equals(""))
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
            EditorGUILayout.PropertyField(guidProperty);

            if (guidProperty.stringValue.Equals(""))
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
}