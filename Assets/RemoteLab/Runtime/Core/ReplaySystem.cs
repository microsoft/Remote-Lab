using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System;
using System.Linq;
using UnityEngine.Video;
using UnityEditor;
using System.Reflection;

namespace RemoteLab
{
    public class ReplaySystem : MonoBehaviour
    {
        public bool replaying;

        private StreamReader reader;
        private StreamReader uiReader;
        private int curFrame;
        private int totalFrames;
        private string entry;
        private string uiEntry;
        private Dictionary<string, GameObject> guidToObject;
        private Dictionary<string, GameObject> guidToUI;
        private Dictionary<string, string> guidMapper;
        private Dictionary<GameObject, string> objectToUI;
        private List<GameObject> instancedObjects;
        private SortedSet<int> frameSet;
        private SortedSet<int> uiFrameSet;
        private Dictionary<int, long> frameToLine;
        private Dictionary<int, long> uiFrameToLine;

        public bool toggleReplay;
        public bool pauseReplay;

        private bool seeking;

        public string transformDataPath = "";
        public string uiDataPath = "";
        public string transformDataFilePrefix = "transform";
        public string uiDataFilePrefix = "ui_event";
        public float frameRate = 60f;
        public int iframeInterval = 250;
        public VideoPlayer videoPlayer;

        private GameObject origReplayCollection;
        private GameObject currReplayCollection;
        private GameObject prevReplayCollection;

        private static ReplaySystem _instance;
        public static ReplaySystem Instance { get { return _instance; } }

        private void Start()
        {
            replaying = false;

            if ((transformDataPath == null || transformDataPath == "") ||
                (uiDataPath == null || uiDataPath == ""))
            {
                string filePath = (Application.isEditor) ? Application.dataPath : Application.persistentDataPath;
                filePath = Path.Combine(filePath, "Recordings");
                List<string> sessionLst = new List<string>(Directory.GetDirectories(filePath));

                if (sessionLst.Count > 0)
                {
                    sessionLst.Sort();
                    filePath = Path.Combine(filePath, sessionLst.Last());
                    List<string> participantLst = new List<string>(Directory.GetDirectories(filePath));

                    if (participantLst.Count > 0)
                    {
                        participantLst.Sort();
                        filePath = Path.Combine(filePath, participantLst.Last());
                        List<string> timesLst = new List<string>(Directory.GetDirectories(filePath));

                        if (timesLst.Count > 0)
                        {
                            List<DateTime> timeStamps = new List<DateTime>();

                            foreach (string tString in timesLst)
                            {
                                string baseName = new DirectoryInfo(tString).Name;
                                string[] splits = baseName.Split('_');
                                splits[0] = splits[0].Replace('-', '/');
                                splits[1] = splits[1].Replace('-', ':');
                                DateTime dateTime = DateTime.Parse(string.Join(" ", splits));
                                timeStamps.Add(dateTime);
                            }

                            timeStamps.Sort();
                            string tStringFile = timeStamps.Last().ToString().Replace(':', '-').Replace('/', '-').Replace(' ', '_');
                            filePath = Path.Combine(filePath, tStringFile);
                            transformDataPath = Path.Combine(filePath, transformDataFilePrefix + "_data.csv");
                            uiDataPath = Path.Combine(filePath, uiDataFilePrefix + "_data.csv");

                            print(transformDataPath + "\t" + uiDataPath);
                        }
                    }
                }
            }

            if ((transformDataPath == null || transformDataPath == "") ||
                (uiDataPath == null || uiDataPath == ""))
            {
                Debug.LogError("Failed to find a recording...");
                return;
            }

            reader = new StreamReader(File.OpenRead(transformDataPath));
            uiReader = new StreamReader(File.OpenRead(uiDataPath));

            instancedObjects = new List<GameObject>();
            guidToObject = new Dictionary<string, GameObject>();
            guidToUI = new Dictionary<string, GameObject>();
            guidMapper = new Dictionary<string, string>();
            objectToUI = new Dictionary<GameObject, string>();
            frameSet = new SortedSet<int>();
            uiFrameSet = new SortedSet<int>();
            frameToLine = new Dictionary<int, long>();
            uiFrameToLine = new Dictionary<int, long>();

            PreprocessFile();
            InvokeRepeating("ReplayRoutine", 1, 1 / frameRate);

            // Attach event to seeking video player
            videoPlayer.seekCompleted += OnSeekComplete;
        }

        void PreprocessFile()
        {
            long position = reader.BaseStream.Position;
            string entry = reader.ReadLine();
            int frame;

            while (entry != null)
            {
                string[] data = entry.Split(',');

                if (!data[0].Equals("FrameCount"))
                {
                    frame = int.Parse(data[0]);

                    if (!frameSet.Contains(frame))
                    {
                        frameSet.Add(frame);
                        frameToLine.Add(frame, position);
                    }
                }

                position = reader.GetPosition();
                entry = reader.ReadLine();
            }

            totalFrames = frameSet.Last();

            position = uiReader.BaseStream.Position;
            entry = uiReader.ReadLine();

            while (entry != null)
            {
                string[] data = entry.Split(',');

                if (!data[0].Equals("FrameCount"))
                {
                    frame = int.Parse(data[0]);

                    if (!uiFrameSet.Contains(frame))
                    {
                        uiFrameSet.Add(frame);
                        uiFrameToLine.Add(frame, position);
                    }
                }

                position = uiReader.GetPosition();
                entry = uiReader.ReadLine();
            }
        }

        void ReplayRoutine()
        {
            HandleReplay();
        }

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
        }

        public void OnPlayPauseClick()
        {
            if (!toggleReplay)
            {
                toggleReplay = true;

                if (videoPlayer)
                    videoPlayer.Play();
            }
            else
            {
                pauseReplay = !pauseReplay;

                if (videoPlayer)
                {
                    if (pauseReplay)
                    {
                        videoPlayer.Pause();
                    }
                    else
                    {
                        videoPlayer?.Play();
                    }
                }
            }
        }

        public void OnClickStop()
        {
            toggleReplay = false;

            if (videoPlayer)
                videoPlayer.Stop();
        }

        public void OnSliderHit(float value)
        {
            int scrubbedFrame = (int)(value * totalFrames);

            // Get closest iframe and then replay from iframe up to scrubbedFrame
            int iframe = iframeInterval * (scrubbedFrame / iframeInterval);
            int idxFrame = 0;

            foreach (int refFrame in frameSet)
            {
                if (refFrame > scrubbedFrame)
                    break;

                idxFrame = refFrame;
            }

            int uiFrame = 0;

            foreach (int refFrame in uiFrameSet)
            {
                if (refFrame > scrubbedFrame)
                    break;

                uiFrame = refFrame;
            }

            ReplayUpToScrubbedFrame(iframe, idxFrame, uiFrame);

            if (videoPlayer)
            {
                int videoScrubbedFrame = (int)(videoPlayer.frameCount * value);
                videoPlayer.frame = videoScrubbedFrame;
                videoPlayer.Play();
                seeking = true;
            }

            curFrame = scrubbedFrame;
        }

        private void OnSeekComplete(VideoPlayer source)
        {
            seeking = false;
        }

        void ReplayUpToScrubbedFrame(int iframe, int refFrame, int uiRefFrame)
        {
            // If there are Recordable events to replay
            if (frameToLine.ContainsKey(iframe))
            {
                print("Replaying from " + iframe + " up to and including " + refFrame);
                print("Using position " + frameToLine[iframe]);
                reader.SetPosition(frameToLine[iframe]);
                entry = reader.ReadLine();

                ReplayIFrame(iframe);

                int frame;

                while (entry != null)
                {
                    string[] data = entry.Split(',');
                    frame = int.Parse(data[0]);

                    if (frame > refFrame)
                        break;

                    entry = reader.ReadLine();
                    HandleObjectReplay(data);
                }
            }
            else
            {
                ClearDictionaries();
                InitReplayCollection();

                var keyLst = guidToObject.Keys.ToList();
                foreach (var key in keyLst)
                {
                    Destroy(guidToObject[key]);
                }
            }

            // If there are UI Events to replay here
            if (uiFrameToLine.ContainsKey(iframe))
            {
                print("Replaying UI from " + iframe + " up to and including " + uiRefFrame);
                print("Using position " + uiFrameToLine[iframe]);
                uiReader.SetPosition(uiFrameToLine[iframe]);
                uiEntry = uiReader.ReadLine();
                ReplayUIIFrame(iframe);

                int frame;

                while (uiEntry != null)
                {
                    string[] data = uiEntry.Split(',');
                    frame = int.Parse(data[0]);

                    if (frame > uiRefFrame)
                        break;

                    uiEntry = uiReader.ReadLine();
                    HandleUIReplay(data);
                }
            }

            toggleReplay = true;
            replaying = true;
            pauseReplay = false;
        }

        void ReplayIFrame(int iframe)
        {
            ClearDictionaries();
            InitReplayCollection();

            int frame;
            HashSet<string> activeGuids = new HashSet<string>();

            while (entry != null)
            {
                string[] data = entry.Split(',');
                frame = int.Parse(data[0]);

                if (frame > iframe)
                    break;

                entry = reader.ReadLine();
                HandleObjectReplay(data);
                activeGuids.Add(data[13]);
            }



            var keyLst = guidToObject.Keys.ToList();
            foreach (var key in keyLst)
            {
                Recordable tr = guidToObject[key].GetComponent<Recordable>();

                if (!activeGuids.Contains(guidMapper[tr.guidString]))
                {
                    print("Removing " + guidToObject[key] + " during scrub");
                    Destroy(guidToObject[key]);
                    guidMapper.Remove(tr.guidString);
                    guidToObject.Remove(key);
                }
            }
        }

        void ReplayUIIFrame(int iframe)
        {
            int frame;

            // Handle UI
            while (uiEntry != null)
            {
                string[] data = uiEntry.Split(',');
                frame = int.Parse(data[0]);

                if (frame > iframe)
                    break;

                uiEntry = uiReader.ReadLine();
                HandleUIReplay(data);
            }
        }

        void HandleReplay()
        {
            if (prevReplayCollection)
            {
                Destroy(prevReplayCollection);
            }

            // Handle   
            if (!toggleReplay)
            {
                replaying = false;
                pauseReplay = false;
                return;
            }

            if (pauseReplay || seeking)
            {
                return;
            }

            // Initialize replay if not done
            if (!replaying)
            {
                InitReplay();

                replaying = true;
            }

            ReplayUI();
            ReplayTransform();

            curFrame += 1;

            if (entry == null && uiEntry == null)
            {
                replaying = false;
                toggleReplay = false;
            }
        }

        void ReplayUI()
        {
            int frame;

            while (uiEntry != null)
            {
                string[] data = uiEntry.Split(',');
                frame = int.Parse(data[0]);

                if (frame != curFrame)
                {
                    break;
                }

                uiEntry = uiReader.ReadLine();

                HandleUIReplay(data);
            }
        }

        void HandleUIReplay(string[] data)
        {
            string uiType, state, guid, uiHierarchy;
            uiType = data[1];
            state = data[2];
            guid = data[4];
            uiHierarchy = data[3];

            GameObject replayUI = null;

            if (guidToUI.ContainsKey(guid))
            {
                replayUI = guidToUI[guid];
            }
            else
            {
                // Try looking for object in the scene
                GameObject foundObject;

                string modifiedPath = "/" + currReplayCollection.name + uiHierarchy;

                if ((foundObject = GameObject.Find(modifiedPath)) != null)
                {
                    replayUI = foundObject;
                    guidToUI[guid] = replayUI;
                }
            }

            if (replayUI == null || !replayUI.activeInHierarchy)
                return;

            var pointer = new PointerEventData(EventSystem.current);

            if (uiType.Equals("button"))
            {
                replayUI.GetComponent<Button>().onClick.RemoveAllListeners();
                ExecuteEvents.Execute(replayUI, pointer, ExecuteEvents.submitHandler);
            }
            else if (uiType.Equals("toggle"))
            {
                replayUI.GetComponent<Toggle>().isOn = state.Equals("True");
                //ExecuteEvents.Execute(replayUI, pointer, ExecuteEvents.pointerClickHandler);
            }
            else if (uiType.Equals("slider"))
            {
                replayUI.GetComponent<Slider>().value = float.Parse(state);
            }
        }

        void ReplayTransform()
        {
            int frame;

            // Process entries for current frame of replay
            while (entry != null)
            {
                // Split each value from entry
                string[] data = entry.Split(',');

                // Check frame
                frame = int.Parse(data[0]);

                if (frame != curFrame)
                    break;

                // Read next entry for later
                entry = reader.ReadLine();

                HandleObjectReplay(data);
            }
        }

        void HandleObjectReplay(string[] data)
        {
            // Get instance info
            string status = data[2];
            string guid = data[13];

            GameObject replayObj = FindReplayObject(data);

            // Verify that object exists
            if (replayObj == null)
            {
                return;
            }

            // Destroy object if necessary
            if (status.Equals(ReplayManager.ObjectStatus.Destroyed.ToString()))
            {
                guidMapper.Remove(guidToObject[guid].GetComponent<Recordable>().guidString);
                guidToObject.Remove(guid);
                Destroy(replayObj);
                return;
            }

            // Handle activate/deactivate
            if (status.Equals(ReplayManager.ObjectStatus.Activated.ToString()))
            {
                replayObj.SetActive(true);
                return;
            }

            if (status.Equals(ReplayManager.ObjectStatus.Deactivated.ToString()) ||
                status.Equals(ReplayManager.ObjectStatus.IFrame_Inactive.ToString()))
            {
                replayObj.SetActive(false);
                return;
            }

            if (status.Equals(ReplayManager.ObjectStatus.IFrame_Active.ToString()))
            {
                replayObj.SetActive(true);
            }

            // Fix object parenting for replay
            FixObjectParenting(replayObj, data);

            // Set transform for replay object
            ReplayTransform(replayObj, data);
        }

        GameObject FindReplayObject(string[] data)
        {
            GameObject replayObj = null;
            string guid = data[13];
            string path = data[14];
            string objName = data[1];
            string resourcePath = data[12];

            // Use object in replay table if exists
            // Pre-existing objects
            if (guidToObject.ContainsKey(guid))
            {
                replayObj = guidToObject[guid];
            }
            // Otherwise, object is instantiated or created during session
            else
            {
                // Create new object if possible and assign
                if (resourcePath.Equals(""))
                {
                    // Try looking for object in the scene (case: instantiated during runtime, but before recording)
                    GameObject foundObject;

                    string modifiedPath = "/" + currReplayCollection.name + path;

                    if ((foundObject = GameObject.Find(modifiedPath)) != null)
                    {
                        guidToObject[guid] = foundObject;
                        guidMapper[foundObject.GetComponent<Recordable>().guidString] = guid;

                        return foundObject;
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    try
                    {
                        GameObject objToInstantiate = Resources.Load(resourcePath, typeof(GameObject)) as GameObject;
                        replayObj = Instantiate(objToInstantiate);

                        if (replayObj == null)
                            return null;

                        instancedObjects.Add(replayObj);
                        replayObj.name = objName;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error instantiating " + resourcePath + " for " + path + "\n" + e.Message);

                        if (replayObj != null)
                            Destroy(replayObj);

                        return null;
                    }
                }

                // Store object info into replay table
                guidToObject[guid] = replayObj;
                guidMapper[replayObj.GetComponent<Recordable>().guidString] = guid;
            }

            return replayObj;
        }

        void FixObjectParenting(GameObject replayObj, string[] data)
        {
            string path = data[14];

            // Find parent
            List<string> hierarchy = new List<string>(path.Split('/'));

            // Remove empty entry
            hierarchy.RemoveAt(0);

            // Handle case if no parent vs. has parent
            // Set parent appropriately
            if (hierarchy.Count <= 1)
            {
                // Set parent to collection
                if (replayObj.transform.parent != currReplayCollection.transform)
                {
                    replayObj.transform.parent = currReplayCollection.transform;
                }
            }
            else
            {
                int rangeCt = hierarchy.Count - 1;
                string newParentPath = "/" + currReplayCollection.name + "/" + string.Join("/", hierarchy.GetRange(0, rangeCt));
                Transform newParent = GameObject.Find(newParentPath)?.transform;

                if (newParent != replayObj.transform.parent)
                {
                    replayObj.transform.SetParent(newParent);
                }
            }
        }

        void ReplayTransform(GameObject replayObj, string[] data)
        {
            // Set transform for replay object
            float posX = float.Parse(data[3]);
            float posY = float.Parse(data[4]);
            float posZ = float.Parse(data[5]);
            float rotX = float.Parse(data[6]);
            float rotY = float.Parse(data[7]);
            float rotZ = float.Parse(data[8]);
            float scalX = float.Parse(data[9]);
            float scalY = float.Parse(data[10]);
            float scalZ = float.Parse(data[11]);

            Vector3 position = new Vector3(posX, posY, posZ);
            Vector3 rotation = new Vector3(rotX, rotY, rotZ);
            Vector3 scale = new Vector3(scalX, scalY, scalZ);

            replayObj.transform.localPosition = position;
            replayObj.transform.localRotation = Quaternion.Euler(rotation);
            replayObj.transform.localScale = scale;
        }

        void InitReplay()
        {
            // Transform Events
            reader.SetPosition(0);
            reader.ReadLine();

            // Clear dictionary
            ClearDictionaries();

            // Init replay collection of objects
            InitReplayCollection();

            // UI Events
            uiReader.SetPosition(0);
            uiReader.ReadLine();

            entry = reader.ReadLine();
            uiEntry = uiReader.ReadLine();
            curFrame = 0;
        }

        public void InitReplayCollection()
        {
            // Get Replay Collection
            if (origReplayCollection == null)
            {
                origReplayCollection = GameObject.Find("ReplayCollection");
                origReplayCollection.SetActive(false);
            }

            // Deactivate previous collection if it exists, delete in next update loop
            prevReplayCollection = currReplayCollection;
            prevReplayCollection?.SetActive(false);

            currReplayCollection = Instantiate(origReplayCollection);
            currReplayCollection.SetActive(true);

            // Get non-instantiated objects i.e. non-inst (active + inactive) trackables in collection (scene)
            Recordable[] trackedObjs = currReplayCollection.GetComponentsInChildren<Recordable>(true);

            foreach (Recordable tracked in trackedObjs)
            {
                if (!tracked.isInstantiatedAtRuntime && !guidToObject.ContainsKey(tracked.guidString))
                {
                    guidToObject.Add(tracked.guidString, tracked.gameObject);
                    guidMapper.Add(tracked.guidString, tracked.guidString);
                }
            }

            // Get non-instanced UIs
            InteractableUI[] trackedUIs = currReplayCollection.GetComponentsInChildren<InteractableUI>(true);

            foreach (InteractableUI trackedUI in trackedUIs)
            {
                if (!guidToUI.ContainsKey(trackedUI.guidString))
                {
                    guidToUI.Add(trackedUI.guidString, trackedUI.gameObject);
                }
            }
        }

        void ClearDictionaries()
        {
            guidToObject.Clear();
            guidMapper.Clear();
            guidToUI.Clear();
        }

        private void OnDestroy()
        {
            reader?.Close();
            uiReader?.Close();

            if (guidToObject != null && guidToObject.Count > 0)
            {
                foreach (KeyValuePair<string, GameObject> entry in guidToObject)
                {
                    Destroy(entry.Value);
                }
            }

            videoPlayer.seekCompleted -= OnSeekComplete;
        }

        public int GetCurFrame()
        {
            return curFrame;
        }

        public int GetTotalFrames()
        {
            return totalFrames;
        }
    }

    public static class StreamReaderExtensions
    {
        readonly static FieldInfo charPosField = typeof(StreamReader).GetField("charPos",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo byteLenField = typeof(StreamReader).GetField("byteLen",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        readonly static FieldInfo charBufferField = typeof(StreamReader).GetField("charBuffer",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        public static long GetPosition(this StreamReader reader)
        {
            int byteLen = (int)byteLenField.GetValue(reader);
            var position = reader.BaseStream.Position - byteLen;

            int charPos = (int)charPosField.GetValue(reader);
            if (charPos > 0)
            {
                var charBuffer = (char[])charBufferField.GetValue(reader);
                var encoding = reader.CurrentEncoding;
                var bytesConsumed = encoding.GetBytes(charBuffer, 0, charPos).Length;
                position += bytesConsumed;
            }

            return position;
        }

        public static void SetPosition(this StreamReader reader, long position)
        {
            reader.DiscardBufferedData();
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
    }
}