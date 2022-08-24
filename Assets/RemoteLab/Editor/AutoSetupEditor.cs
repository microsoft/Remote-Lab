using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace RemoteLab.Editor
{
    [CustomEditor(typeof(AutoSetup))]
    public class AutoSetupEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Run Replay Setup"))
            {
                Debug.Log("Setting up replay scene");
                ((AutoSetup)target).SetupReplay();
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }
}
