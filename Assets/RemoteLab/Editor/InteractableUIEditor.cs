using UnityEngine;
using UnityEditor;

namespace RemoteLab.Editor
{
    [CustomEditor(typeof(InteractableUI)), CanEditMultipleObjects]
    public class InteractableUIEditor : UnityEditor.Editor
    {
        private System.Guid generatedGuid;

        private void OnEnable()
        {
            generatedGuid = System.Guid.Empty;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var guidProperty = serializedObject.FindProperty("guidString");

            if (GUILayout.Button("Generate GUID"))
            {
                generatedGuid = System.Guid.NewGuid();
                guidProperty.stringValue = generatedGuid.ToString();
            }

            EditorGUILayout.PropertyField(guidProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}