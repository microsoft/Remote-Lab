using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;

namespace RemoteLab
{
    public class ReplayManager : MonoBehaviour
    {
        private StreamWriter transformDataWriter;
        private StreamWriter UIEventDataWriter;
        private StreamWriter customVariableDataWriter;

        private string transformDataSavePath;
        private string UIEventDataSavePath;
        private string customVariableDataSavePath;
        private string saveFolderPath;

        private static ReplayManager _instance;

        public static ReplayManager Instance { get { return _instance; } }

        public enum ObjectStatus
        {
            Instantiated, Changed, Destroyed, Activated, Deactivated, IFrame_Active,
            IFrame_Inactive
        }

        public string transformDataFilePrefix = "transform";
        public string UIEventDataFilePrefix = "ui_event";
        public string customVariableDataFilePrefix = "custom_variables";
        public string replayFilePrefix = "replay";

        public string sessionId = "session_1";
        public string participantId = "participant_1";

        private ObsManager obsManager;
        private bool awaitingOBS;

        public bool startStopRecording;
        public bool recording;
        public bool recordOBS;

        public float frameRate = 60;
        public ulong iframeInterval = 250;

        private ulong frameOffset;

        public HashSet<Recordable> trackables;
        public HashSet<InteractableUI> interactableUIs;

        private bool startUnityRecord;
        private bool stopUnityRecord;

        // Frame Counts
        private ulong currFrameCount;

        // Networking
        public NetTransfer netTransfer;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }

            trackables = new HashSet<Recordable>();
            interactableUIs = new HashSet<InteractableUI>();
        }

        private void Start()
        {
            obsManager = GetComponent<ObsManager>();
            currFrameCount = 0;
            float repeatInterval = 1 / frameRate;
            InvokeRepeating("UpdateFrame", 0.1f, repeatInterval);

            // Attach handlers to ObsManager
            obsManager.ConnectedToOBS += OnConnectedToOBS;
            obsManager.DisconnectedFromOBS += OnDisconnectedFromOBS;
            obsManager.StartedOBS += OnOBSRecordingStarted;
            obsManager.StoppedOBS += OnOBSRecordingStopped;
            obsManager.FailedOBSStart += OnFailedOBSStart;
        }

        private void UpdateFrame()
        {
            currFrameCount += 1;

            if (recording && (currFrameCount - frameOffset) % iframeInterval == 0)
            {
                // Write in i-frame data
                LogTrackableIFrame(false);
                LogAllInteractableUI();
            }
        }

        private void Update()
        {
            if (startStopRecording && !awaitingOBS)
            {
                startStopRecording = false;

                // Broadcast start/stop recording
                if (netTransfer != null)
                {
                    netTransfer.SendStartStopToClients();
                }

                HandleRecording();
            }

            if (awaitingOBS)
            {
                if (startUnityRecord)
                {
                    StartUnityRecord();
                    startUnityRecord = false;
                    awaitingOBS = false;
                }
                else if (stopUnityRecord)
                {
                    StopUnityRecord();
                    stopUnityRecord = false;
                    awaitingOBS = false;
                }
            }
        }

        public void HandleRecording()
        {
            if (recording)
            {
                // Stop recording
                if (recordOBS)
                {
                    StopRecordWithOBS();
                }
                else
                {
                    StopUnityRecord();
                }
            }
            else
            {
                // Clear tracking lists?
                trackables.Clear();
                interactableUIs.Clear();

                // Record Trackables into list
                RegisterAllTrackables();
                RegisterAllInteractableUIs();

                if (recordOBS)
                {
                    StartRecordWithOBS();
                }
                else
                {
                    StartUnityRecord();
                }
            }
        }

        private void OnDestroy()
        {
            CloseAllWriters();
        }

        private void RegisterAllTrackables()
        {

#if UNITY_EDITOR

            foreach (Recordable tr in Resources.FindObjectsOfTypeAll(typeof(Recordable)) as Recordable[])
            {
                if (!EditorUtility.IsPersistent(tr.gameObject.transform.root.gameObject) && !(tr.gameObject.hideFlags == HideFlags.NotEditable || tr.gameObject.hideFlags == HideFlags.HideAndDontSave))
                {
                    trackables.Add(tr);
                    tr.registeredToManager = true;
                }
            }

#else
        
        UnityEngine.Debug.LogError("Cannot record all Trackables from non-editor client");

        foreach (Trackable tr in GameObject.FindObjectsOfType<Trackable>())
        {
            trackables.Add(tr);
            tr.registeredToManager = true;
        }

#endif

        }

        private void RegisterAllInteractableUIs()
        {
#if UNITY_EDITOR

            foreach (InteractableUI ui in Resources.FindObjectsOfTypeAll(typeof(InteractableUI)) as InteractableUI[])
            {
                if (!EditorUtility.IsPersistent(ui.gameObject.transform.root.gameObject) && !(ui.gameObject.hideFlags == HideFlags.NotEditable || ui.gameObject.hideFlags == HideFlags.HideAndDontSave))
                {
                    interactableUIs.Add(ui);
                    ui.registeredToManager = true;
                }
            }

#else

        UnityEngine.Debug.LogError("Cannot record all InteractableUIs from non-editor client");

        foreach (InteractableUI ui in GameObject.FindObjectsOfType<InteractableUI>())
        {
            interactableUIs.Add(ui);
            ui.registeredToManager = true;
        }

#endif
        }

        private void LogTrackableIFrame(bool initialFrame)
        {
            foreach (Recordable t in trackables)
            {
                if (t.gameObject.activeInHierarchy)
                {
                    ObjectStatus status = (initialFrame) ? ObjectStatus.Instantiated : ObjectStatus.IFrame_Active;
                    WriteTransformDataEntry(t.transform, status, t.guidString, t.resourceName);
                }
                else
                {
                    ObjectStatus status = (initialFrame) ? ObjectStatus.Deactivated : ObjectStatus.IFrame_Inactive;
                    WriteTransformDataEntry(t.transform, status, t.guidString, t.resourceName);
                }
            }
        }

        private void LogFinalFrame()
        {
            // Log all trackables including inactive ones
            foreach (Recordable t in trackables)
            {
                WriteTransformDataEntry(t.transform, ObjectStatus.Destroyed, t.guidString, t.resourceName);
            }
        }

        private void LogAllInteractableUI()
        {
            foreach (InteractableUI i in interactableUIs)
            {
                i.WriteStateEntry();
            }
        }

        private void SetLoggedStartForTrackables(bool start)
        {
            foreach (Recordable t in trackables)
            {
                t.loggedStart = start;
            }
        }

        private void InitializeRecording()
        {
            string platformPath = (Application.isEditor) ? Application.dataPath : Application.persistentDataPath;

            platformPath = Path.Combine(platformPath, "Recordings", sessionId, participantId);

            string timeStamp = System.DateTime.Now.ToString();
            string invalidChars = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalidChars)
            {
                timeStamp = timeStamp.Replace(c, '-');
            }

            timeStamp = timeStamp.Replace(' ', '_');
            platformPath = Path.Combine(platformPath, timeStamp);
            saveFolderPath = platformPath;

            Directory.CreateDirectory(platformPath);

            // Initialize Transform Data Recording
            transformDataSavePath = platformPath + "/" + transformDataFilePrefix + "_data.csv";
            transformDataWriter = new StreamWriter(transformDataSavePath);

            string[] transformDataHeaders = { "FrameCount", "GameObject", "Status", "Pos X", "Pos Y", "Pos Z", "Rot X", "Rot Y",
                                          "Rot Z", "Scal X", "Scal Y", "Scal Z", "Resource Path", "ID", "Hierarchy" };
            transformDataWriter.WriteLine(string.Join(",", transformDataHeaders));

            // Intialize UI Event Data Recording
            UIEventDataSavePath = platformPath + "/" + UIEventDataFilePrefix + "_data.csv";
            UIEventDataWriter = new StreamWriter(UIEventDataSavePath);

            string[] UIEventDataHeaders = { "FrameCount", "UI Type", "New Value", "Hierarchy", "ID" };
            UIEventDataWriter.WriteLine(string.Join(",", UIEventDataHeaders));

            // Intialize Custom Variable Data Recording
            customVariableDataSavePath = platformPath + "/" + customVariableDataFilePrefix + "_data.csv";
            customVariableDataWriter = new StreamWriter(customVariableDataSavePath);

            string[] customVariableDataHeaders = { "FrameCount", "Class Name", "Variable Name", "Variable Value" };
            customVariableDataWriter.WriteLine(string.Join(",", customVariableDataHeaders));
        }

        private void StartUnityRecord()
        {
            print("Called StartUnityRecord");
            InitializeRecording();
            //frameOffset = Time.frameCount;
            frameOffset = currFrameCount;
            recording = true;
            //LogAllTrackables(ObjectStatus.Instantiated);

            LogTrackableIFrame(true);
            LogAllInteractableUI();
            SetLoggedStartForTrackables(true);
        }

        private void StopUnityRecord()
        {
            LogFinalFrame();
            LogAllInteractableUI();

            recording = false;
            SetLoggedStartForTrackables(false);
            CloseAllWriters();
        }

        private void StartRecordWithOBS()
        {
            if (obsManager != null)
            {
                print("Starting OBS Process...");
                obsManager.StartOBS();
                awaitingOBS = true;
            }
        }

        private void StopRecordWithOBS()
        {
            if (obsManager != null)
            {
                obsManager.StopOBSRecording();
                awaitingOBS = true;
            }
        }

        private void OnConnectedToOBS()
        {
            // Start the OBS recording on connection
            obsManager.StartOBSRecording();
        }

        private void OnDisconnectedFromOBS()
        {
            // Handle disconnect cases
            awaitingOBS = false;
        }

        private void OnOBSRecordingStarted()
        {
            print("Starting Unity Recording");
            startUnityRecord = true;
        }

        private void OnOBSRecordingStopped()
        {
            print("Stopping Unity Recording");
            stopUnityRecord = true;
        }

        private void OnFailedOBSStart()
        {
            awaitingOBS = false;
        }

        public void CloseAllWriters()
        {
            transformDataWriter?.Close();
            UIEventDataWriter?.Close();
            customVariableDataWriter?.Close();
        }

        public void WriteTransformDataEntry(Transform transform, ObjectStatus objState, string guid, string resourceName = "")
        {
            if (!recording)
                return;

            //int frameCount = Time.frameCount - frameOffset;
            ulong frameCount = currFrameCount - frameOffset;

            float posX = transform.localPosition.x;
            float posY = transform.localPosition.y;
            float posZ = transform.localPosition.z;

            Vector3 rot = transform.localRotation.eulerAngles;
            float rotX = rot.x;
            float rotY = rot.y;
            float rotZ = rot.z;

            float scalX = transform.localScale.x;
            float scalY = transform.localScale.y;
            float scalZ = transform.localScale.z;


            string[] values = { frameCount.ToString(), transform.name, objState.ToString(),
                            posX.ToString(), posY.ToString(), posZ.ToString(),
                            rotX.ToString(), rotY.ToString(), rotZ.ToString(),
                            scalX.ToString(), scalY.ToString(), scalZ.ToString(),
                            resourceName, guid,
                            GetGameObjectPath(transform.gameObject) };

            string entry = string.Join(",", values);

            transformDataWriter.WriteLine(entry);
        }

        private string GetGameObjectPath(GameObject obj)
        {
            string path = "/" + obj.name;
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
                path = "/" + obj.name + path;
            }
            return path;
        }

        public void WriteUIEventDataEntry(string uiType, string new_val, GameObject uiObject, string guid)
        {
            if (!recording)
                return;

            //int frameCount = Time.frameCount - frameOffset;
            ulong frameCount = currFrameCount - frameOffset;

            string[] values = { frameCount.ToString(), uiType, new_val,
                            GetGameObjectPath(uiObject), guid };
            string entry = string.Join(",", values);

            UIEventDataWriter.WriteLine(entry);
        }

        public void WriteCustomVariableDataEntry<T>(string class_name, string var_name, T var_val)
        {
            if (!recording)
                return;

            //int frameCount = Time.frameCount - frameOffset;
            ulong frameCount = currFrameCount - frameOffset;

            string[] values = { frameCount.ToString(), class_name, var_name, var_val.ToString() };
            string entry = string.Join(",", values);

            customVariableDataWriter.WriteLine(entry);
        }

        public string GetSaveFolderPath()
        {
            return saveFolderPath;
        }

        public string GetTransformDataPath()
        {
            return transformDataSavePath;
        }

        public string GetUIDataPath()
        {
            return UIEventDataSavePath;
        }
    }
}