using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using UnityEditor.PackageManager;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEditorInternal;

using Photon.Pun;

namespace RemoteLab.Editor
{
    public class RemoteLabEditor : EditorWindow
    {
        public static RemoteLabEditor Instance { get; private set; }
        public static bool IsOpen
        {
            get { return Instance != null; }
        }

        private readonly Vector2 preferredSize = new Vector2(450, 600);
        Vector2 scrollPos;

        private static Dictionary<string, bool> _installedAssets;
        private static UnityEditor.PackageManager.Requests.AddRequest _addRequest;
        private static ReplayManager _sceneReplayManager;
        private static AutoSetup _sceneAutoSetup;
        private static NetTransfer _sceneNetTransfer;
        private static bool _isReplayScene;
        private static List<Recordable> _sceneRecordables;
        private static List<NetVRPlayer> _scenePlayers;

        private static bool _doFirstRun = true;
        private static bool _showedFirstRun = false;
        private static bool _toggleObs = false;

        public RemoteLabEditor()
        {
            minSize = preferredSize;
            if (_doFirstRun) EditorApplication.update += CheckFirstRun;
        }

        [InitializeOnLoadMethod]
        public static void InitializeOnLoadMethod()
        {
            EditorApplication.delayCall += OnDelayCall;
        }


        // used to register for various events (post-load)
        private static void OnDelayCall()
        {
            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

#if UNITY_2021_1_OR_NEWER
            CompilationPipeline.compilationStarted -= OnCompileStarted;
            CompilationPipeline.compilationStarted += OnCompileStarted;
#else
            CompilationPipeline.assemblyCompilationStarted -= OnCompileStarted;
            CompilationPipeline.assemblyCompilationStarted += OnCompileStarted;
#endif

#if (UNITY_2018 || UNITY_2018_1_OR_NEWER)
            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
#else
            EditorApplication.projectWindowChanged -= OnProjectChanged;
            EditorApplication.projectWindowChanged += OnProjectChanged;
#endif

            // TODO: does 2021 also change this callback?
            EditorApplication.hierarchyChanged -= OnProjectChanged;
            EditorApplication.hierarchyChanged += OnProjectChanged;


            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                OnProjectChanged();
            }
        }

        static void PlayModeStateChanged(PlayModeStateChange state)
        {
            UpdateRemoteLabData();
        }

        static void OnCompileStarted(string action)
        {
            UpdateRemoteLabData();
        }

        static void OnProjectChanged()
        {
            UpdateRemoteLabData();
        }

        [MenuItem("RemoteLab/Control Panel", false, 0)]
        static void MenuItemOpenWizard()
        {
            RemoteLabEditor win = GetWindow<RemoteLabEditor>(false, "RemoteLab", true);
            if (win == null) return;
        }

        static void CheckFirstRun()
        {
            EditorApplication.update -= CheckFirstRun;
            if (!EditorPrefs.HasKey("RemoteLab.FirstRun")) DoFirstRun();
        }

        static void DoFirstRun()
        {
            EditorPrefs.SetBool("RemoteLab.FirstRun", true);

            RemoteLabEditor win = GetWindow<RemoteLabEditor>(false, "RemoteLab", true);
            if (win == null) return;

            _showedFirstRun = true;
            _doFirstRun = false;
        }

        static void UpdateRemoteLabData()
        {
            CheckInstalledPackages(new string[] { "Photon", "Oculus" });
            GetAllRecordables();
            GetAllPlayers();
            _sceneReplayManager = FindObjectOfType<ReplayManager>(true);
            _sceneAutoSetup = FindObjectOfType<AutoSetup>(true);
            _sceneNetTransfer = FindObjectOfType<NetTransfer>(true);
            _isReplayScene = GameObject.Find("ReplayCollection") != null; // TODO: maybe a bit too expensive
        }

        [DidReloadScripts]
        static void RecompiledScripts()
        {
            if (IsOpen)
            {
                UpdateRemoteLabData();
            }
        }

        void OnEnable()
        {
            Instance = this;
            UpdateRemoteLabData();
        }

        void OnGUI()
        {
            ControlPanel();

            // DEBUG, remove once done
            //EditorGUILayout.Separator();
            //using (var scope = new GUILayout.HorizontalScope())
            //{
            //    if (GUILayout.Button("Clear First Run", EditorStyles.miniButton))
            //    {
            //        EditorPrefs.DeleteKey("RemoteLab.FirstRun");
            //        _doFirstRun = true;
            //        _showedFirstRun = false;
            //    }
            //}
        }

        void OnInspectorUpdate()
        {
            ProcessAddRequest();
            Repaint();
        }

        void ControlPanel()
        {
            // Title
            GUIStyle bgStyle = EditorGUIUtility.isProSkin ? new GUIStyle(GUI.skin.GetStyle("Label")) : new GUIStyle(GUI.skin.GetStyle("WhiteLabel"));
            bgStyle.padding = new RectOffset(10, 10, 10, 10);
            bgStyle.fontSize = 22;
            bgStyle.fontStyle = FontStyle.Bold;
            bgStyle.alignment = TextAnchor.MiddleCenter;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            Rect scale = GUILayoutUtility.GetLastRect();
            scale.height = 42;

            GUI.Label(scale, "RemoteLab", bgStyle);
            GUILayout.Space(scale.height);

            // First run
            if (_showedFirstRun)
            {
                EditorGUILayout.HelpBox("Thanks for installing RemoteLab! Please dock this control panel anywhere on your Editor. This panel will be a quick access menu to all RemoteLab's features.", MessageType.Info);
            }
            DrawUILine(Color.gray);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // TODO: different UIs for editor scene, active scene, and replay scene

            if (_installedAssets.Any((x) => !x.Value))
            {
                RequiredAssets();
            }
            else if (!_isReplayScene && !EditorApplication.isPlaying) // authoring experiment
            {
                ActiveSceneDetails();
                DrawUILine(Color.gray);
                Players();
                DrawUILine(Color.gray);
                Recordables();
                DrawUILine(Color.gray);
                QuickSetupScene();
                DrawUILine(Color.gray);
                QuickAddGameObject();
                DrawUILine(Color.gray);
                AddQuestionnare();
                DrawUILine(Color.gray);
                SceneActions();
                DrawUILine(Color.gray);
            }
            else if (!_isReplayScene && EditorApplication.isPlaying) // active experiment
            {
                ActiveSceneDetails();
                DrawUILine(Color.gray);
                Players();
                DrawUILine(Color.gray);
                Recordables();
                DrawUILine(Color.gray);
                RunQuestionnaire();
                DrawUILine(Color.gray);
                SceneActions();
                DrawUILine(Color.gray);
            }
            else if (_isReplayScene && !EditorApplication.isPlaying) // replay standby
            {
                ReplaySceneDetails();
                DrawUILine(Color.gray);
                SceneActions();
                DrawUILine(Color.gray);
            }
            else // playing replay
            {

            }

            EditorGUILayout.EndScrollView();            
        }

        void RequiredAssets()
        {
            GUILayout.Label("Required Assets", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            if (_installedAssets.Any((x) => !x.Value))
            {
                GUILayout.Space(2);
                EditorGUILayout.HelpBox("RemoteLab requires the following assets to function correctly. You may see compilation errors until all required assets are installed.", MessageType.Error);
            }

            EditorGUILayout.Separator();

            GUILayout.Label($"Unity XR Plug-in Management: {GetLabel(_installedAssets["XRManagement"], "Installed", "Not Installed", "green", "red")}", new GUIStyle(EditorStyles.label) { richText = true });
            if (!_installedAssets["XRManagement"] && _addRequest == null)
            {
                if (GUILayout.Button("Install Unity XR Plug-In Management", EditorStyles.miniButton))
                {
                    EditorApplication.isPlaying = false;
                    _addRequest = Client.Add("com.unity.xr.management@4.0.6"); // TODO: check if we need a specific version
                }
            }

            GUILayout.Label($"Unity XR Oculus Plug-in: {GetLabel(_installedAssets["XROculus"], "Installed", "Not Installed", "green", "red")}", new GUIStyle(EditorStyles.label) { richText = true });
            if (!_installedAssets["XROculus"] && _addRequest == null)
            {
                if (GUILayout.Button("Install Unity XR Oculus Plug-In", EditorStyles.miniButton))
                {
                    EditorApplication.isPlaying = false;
                    _addRequest = Client.Add("com.unity.xr.oculus@1.9.1");
                }
            }

            GUILayout.Label($"TextMeshPro: {GetLabel(_installedAssets["TextMeshPro"], "Installed", "Not Installed", "green", "red")}", new GUIStyle(EditorStyles.label) { richText = true });
            if (!_installedAssets["TextMeshPro"])
            {
                if (GUILayout.Button("Install TextMeshPro", EditorStyles.miniButton))
                {
                    EditorApplication.isPlaying = false;
                    _addRequest = Client.Add("com.unity.textmeshpro@3.0.6");
                }
            }

            GUILayout.Label($"Photon Unity Network 2: {GetLabel(_installedAssets["Photon"], "Installed", "Not Installed", "green", "red")}", new GUIStyle(EditorStyles.label) { richText = true });
            if (!_installedAssets["Photon"])
            {
                GetLink("Download PUN 2", "https://assetstore.unity.com/packages/tools/network/pun-2-free-119922", EditorStyles.miniButton);
            }

            GUILayout.Label($"Oculus Integration: {GetLabel(_installedAssets["Oculus"], "Installed", "Not Installed", "green", "red")}", new GUIStyle(EditorStyles.label) { richText = true });
            if (!_installedAssets["Oculus"])
            {
                GetLink("Download Oculus Integration", "https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022", EditorStyles.miniButton);
            }
        }

        void Recordables()
        {
            GUILayout.Label($"Recordables {(!EditorApplication.isPlaying ? "(from Scene)" : "(from ReplayManager)")}", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            if (_sceneReplayManager == null)
            {
                EditorGUILayout.HelpBox("No ReplayManager in the scene! Recordables will not record.", MessageType.Warning);
                GUILayout.Space(2);
            }
            else if (!_sceneReplayManager.gameObject.activeSelf || !_sceneReplayManager.enabled)
            {
                EditorGUILayout.HelpBox("ReplayManager is not enabled. Recordables will not record.", MessageType.Warning);
                GUILayout.Space(2);
            }

            if (!EditorApplication.isPlaying)
            {
                for (int i = 0; i < _sceneRecordables.Count; i++)
                {
                    using (var scope = new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(_sceneRecordables[i], typeof(Recordable), true, GUILayout.ExpandWidth(true));
                        //_recordables[i].enabled = EditorGUILayout.Toggle("", _recordables[i].enabled, GUILayout.ExpandWidth(false));
                        if (GUILayout.Button("  X  ", EditorStyles.miniButtonRight, GUILayout.ExpandWidth(false)))
                        {
                            Undo.DestroyObjectImmediate(_sceneRecordables[i]);
                            UpdateRemoteLabData();
                        }
                    }
                }
            }
            else
            {
                foreach (Recordable rec in ReplayManager.Instance.trackables)
                {
                    EditorGUILayout.ObjectField(rec, typeof(Recordable), true);
                }
            }
        }

        void Players()
        {
            GUILayout.Label("Players", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            if (!EditorApplication.isPlaying || NetworkManager.Instance == null)
            {
                GUILayout.Label("Start the experiment to track networked players.");
                return;
            }

            //for (int i = 0; i < NetworkManager.Instance.ConnectedPlayers.Count; i++)
            foreach (var p in NetworkManager.Instance.ConnectedPlayers)
            {
                /*
                 * TODO:
                 * editing the name field should propagate to other clients
                 * sort by PhotonNetwork ID
                 */
                using (var scope = new GUILayout.HorizontalScope())
                {
                    GUILayout.Label($"<b>{p.Value.ActorNumber}</b>", new GUIStyle("Label") { richText = true });
                    GUILayout.Label($"<b>{p.Key}</b>", new GUIStyle("Label") { richText = true });
                    //EditorGUILayout.ObjectField(NetworkManager.Instance.ConnectedPlayers[i], typeof(GameObject), true);
                    EditorGUILayout.TextField(p.Value.NickName); // TODO: propagate changes to players
                }
            }
        }

        void ActiveSceneDetails()
        {
            // details about the scene
            GUILayout.Label("Scene Details", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            GUILayout.Label($"Active Scene: <b>{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            GUILayout.Label($"Scene Running: <b>{GetLabel(EditorApplication.isPlaying, "Yes", "No", "green")}</b>", new GUIStyle(EditorStyles.label) { richText = true });
            if (EditorApplication.isPlaying) GUILayout.Label($"Connected Participants: <b>{(!EditorApplication.isPlaying || NetworkManager.Instance == null ? 0 : NetworkManager.Instance.ConnectedPlayers.Count)}</b> (updates every 5 seconds)", new GUIStyle(EditorStyles.label) { richText = true });
        }

        void ReplaySceneDetails()
        {
            // details about the scene
            // TODO: some information about whether the scene is now a replay scene
            GUILayout.Label("Replay Scene Details", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            GUILayout.Label($"Replay Scene: <b>{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}</b>", new GUIStyle(EditorStyles.label) { richText = true });
        }

        void QuickSetupScene()
        {
            GUILayout.Label("Add RemoteLab Manager to Scene", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            GUILayoutOption buttonStyle = GUILayout.Width(100);

            // TODO: see if this can be undo'd
            static void InstantiateManagerIfNotInScene(string resourcePath, string name, Type type)
            {
                MonoBehaviour m = (MonoBehaviour)FindObjectOfType(type, true);
                // TODO: add replay manager prefab if not in scene
                if (m == null)
                {
                    GameObject g = Resources.Load<GameObject>(resourcePath);
                    GameObject i = Instantiate(g, Vector3.zero, Quaternion.identity);
                    i.name = name;
                    EditorGUIUtility.PingObject(i);
                }
                else
                {
                    EditorGUIUtility.PingObject(m.gameObject);
                }
            }

            // Instantiate prefabs etc
            using (var scope = new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Network", buttonStyle))
                {
                    InstantiateManagerIfNotInScene("Prefabs/Multiplayer/NetworkManager", "NetworkManager", typeof(NetworkManager));
                }
                if (GUILayout.Button("Questionnaire", buttonStyle))
                {
                    InstantiateManagerIfNotInScene("Prefabs/Questionnaires/QuestionnaireManager", "QuestionnaireManager", typeof(SpawnQuestionnaires));
                }
                if (GUILayout.Button("Transfer", buttonStyle))
                {
                    InstantiateManagerIfNotInScene("Prefabs/Multiplayer/TransferManager", "TransferManager", typeof(NetTransfer));
                }
                if (GUILayout.Button("Replay", buttonStyle))
                {
                    InstantiateManagerIfNotInScene("Prefabs/Core/ReplayManager", "ReplayManager", typeof(ReplayManager));
                }
                GUILayout.FlexibleSpace();
            }
        }

        void QuickAddGameObject()
        {
            GUILayout.Label("Add RemoteLab Component to GameObject", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            if (Selection.gameObjects.Length == 0)
            {
                GUILayout.Label("Select a GameObject to add a component.");
                return;
            }

            GUILayout.Label($"Add to: <b>{(Selection.gameObjects.Length > 1 ? (Selection.gameObjects.Length + " GameObjects") : Selection.activeGameObject.name)}</b>", new GUIStyle(EditorStyles.label) { richText = true, wordWrap = true });

            GUILayout.Space(2);
            using (var scope = new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Recordable", EditorStyles.miniButton))
                {
                    for (int i = 0; i < Selection.gameObjects.Length; i++)
                    {
                        Recordable r = Undo.AddComponent<Recordable>(Selection.gameObjects[i]);
                        if (r) r.enabled = true;
                    }

                    UpdateRemoteLabData();
                }
            }
        }

        void AddQuestionnare()
        {
            GUILayout.Label("Add Questionnaire", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            GUILayoutOption buttonStyle = GUILayout.Width(135);

            if (GUILayout.Button("New Questionnaire", EditorStyles.miniButton))
            {
                QuestionnaireContent q = CreateInstance<QuestionnaireContent>();
                EditorUtility.FocusProjectWindow();
                ProjectWindowUtil.CreateAsset(q, "Assets/RemoteLab/Questionnaire.asset");
                // TODO: figure out if it's possible to undo this
            }

            using (var scope = new GUILayout.VerticalScope("box"))
            {
                using (var scope2 = new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Templates", new GUIStyle("Label") { alignment = TextAnchor.MiddleCenter });
                }
                using (var scope3 = new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Likert", buttonStyle))
                    {
                        ProjectWindowUtil.CreateScriptAssetFromTemplateFile("Assets/RemoteLab/Resources/Prefabs/Questionnaires/Examples/Likert.asset", "Assets/RemoteLab/Likert.asset");
                        EditorUtility.FocusProjectWindow();
                    }
                    if (GUILayout.Button("SUS", buttonStyle))
                    {
                        ProjectWindowUtil.CreateScriptAssetFromTemplateFile("Assets/RemoteLab/Resources/Prefabs/Questionnaires/Examples/SUS.asset", "Assets/RemoteLab/SUS.asset");
                        EditorUtility.FocusProjectWindow();
                    }
                    if (GUILayout.Button("NASA-TLX", buttonStyle))
                    {
                        ProjectWindowUtil.CreateScriptAssetFromTemplateFile("Assets/RemoteLab/Resources/Prefabs/Questionnaires/Examples/NASA_TLX.asset", "Assets/RemoteLab/NASA-TLX.asset");
                        EditorUtility.FocusProjectWindow();
                    }
                    GUILayout.FlexibleSpace();
                }
            }
        }

        void RunQuestionnaire()
        {
            GUILayout.Label("Run Questionnaire", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            // TODO: grab questionnaire manager, check all questionnaires to be run, render a button for each
        }

        void SceneActions()
        {
            GUILayout.Label("Experiment Actions", new GUIStyle("Label") { wordWrap = true, fontSize = 16 });
            EditorGUILayout.Separator();

            GUILayoutOption buttonStyle = GUILayout.Width(135);

            using (var scope = new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(!EditorApplication.isPlaying ? "Start Experiment" : "Stop Experiment", buttonStyle))
                {
                    EditorApplication.isPlaying = !EditorApplication.isPlaying;
                }
                if (GUILayout.Button("Build Experiment", buttonStyle))
                {
                    BuildPlayerWindow.ShowBuildPlayerWindow();
                }
                if (GUILayout.Button("Setup Replay", buttonStyle))
                {
                    // TODO: this operation is destructive, could there be a way to allow undo on accidental click
                    EditorApplication.isPlaying = false;
                    _sceneAutoSetup.SetupReplay(); 
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Separator();

            using (var scope = new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(!_sceneReplayManager.recording ? "Start Recording" : "Stop Recording", buttonStyle))
                {
                    _sceneReplayManager.startStopRecording = true;
                }
                _toggleObs = GUILayout.Toggle(_toggleObs, "Use OBS", GUILayout.Width(70));
                _sceneReplayManager.recordOBS = _toggleObs;

                GUILayout.Space(20);
                if (GUILayout.Button("Start Data Transfer", buttonStyle))
                {
                    _sceneNetTransfer.triggerTransfer = true;
                }

                GUILayout.FlexibleSpace();
            }
        }

        #region Helpers

        static void CheckInstalledPackages(string[] assetDirectories)
        {
            /*
             * Check installations
             * "Photon/" directory for PUN 2
             * "Oculus/" directory for Oculus Integration
             * Also check package dependencies (UPM: installed automatically; .unitypackage: needs manual install)
             */

            // TODO: search for Oculus and Photon folders if not in the right place? key file, dll?
            Dictionary<string, bool> installed = new Dictionary<string, bool>();
            string assets = Application.dataPath;
            for (int i = 0; i < assetDirectories.Length; i++)
            {
                bool exists = false;
                if (Directory.Exists(Path.Combine(assets, assetDirectories[i]))) exists = true;

                installed.Add(assetDirectories[i], exists);
            }

            installed.Add("XRManagement", IsPackageInstalled("com.unity.xr.management"));
            installed.Add("XROculus", IsPackageInstalled("com.unity.xr.oculus"));
            installed.Add("TextMeshPro", IsPackageInstalled("com.unity.textmeshpro"));

            _installedAssets = installed;
        }

        static void GetAllRecordables()
        {
            // get recordables in scene
            // TODO: probably too much gc making a list every time
            _sceneRecordables = new List<Recordable>(FindObjectsOfType<Recordable>());
        }

        static void GetAllPlayers()
        {
            _scenePlayers = new List<NetVRPlayer>(FindObjectsOfType<NetVRPlayer>());
        }

        static string GetLabel(bool active, string trueLabel = "True", string falseLabel = "False", string trueColor = "", string falseColor = "")
        {
            if (active) return $"<color={trueColor}><b>{trueLabel}</b></color>";
            else return $"<color={falseColor}><b>{falseLabel}</b></color>";
        }

        static void GetLink(string label, string url, GUIStyle style)
        {
            if (GUILayout.Button(label, style))
            {
                Application.OpenURL(url);
            }
        }

        static void DrawUILine(Color color, int thickness = 1, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height = thickness;
            r.y += padding / 2;
            EditorGUI.DrawRect(r, color);
        }

        public static bool IsPackageInstalled(string packageId)
        {
            if (!File.Exists("Packages/manifest.json"))
                return false;

            string jsonText = File.ReadAllText("Packages/manifest.json");
            return jsonText.Contains(packageId);
        }

        static void ProcessAddRequest()
        {
            if (_addRequest != null)
            {
                if (_addRequest.IsCompleted)
                {
                    if (_addRequest.Status == StatusCode.Failure) Debug.Log(_addRequest.Error.message);
                    else Debug.Log($"{_addRequest.Result.name}@{_addRequest.Result.version}");
                    _addRequest = null;
                }
            }
        }
        #endregion
    }
}