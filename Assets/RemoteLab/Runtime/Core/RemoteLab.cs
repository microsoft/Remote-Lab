using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteLab
{
    /// <summary>
    /// Class to store all information pertaining to a RemoteLab host (which only runs in the editor).
    /// </summary>

    public class RemoteLab : MonoBehaviour
    {
        // Instance
        private static RemoteLab _instance;

        // Managers
        public static ReplayManager ReplayManager { get; private set; }

        public static bool IsHost { get; private set; }
        public static bool IsInitialized { get; private set; } = false;

        public static List<Recordable> Recordables;
        public static Dictionary<int, (string, GameObject)> Players;

        private void Start()
        {
            _instance = this;

#if UNITY_EDITOR
            IsHost = true;
            Initialize();
#else
            IsHost = false;
#endif
        }

        private void OnDestroy()
        {
            _instance = null;
            IsInitialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private void Initialize()
        {
            if (!IsHost) return;
            if (IsInitialized) return;

            // Get managers
            ReplayManager = ReplayManager.Instance;

            Recordables = new List<Recordable>();
            Players = new Dictionary<int, (string, GameObject)>();

            IsInitialized = true;
            DontDestroyOnLoad(this);
        }

        public static void AddRecordable(Recordable r)
        {
            if (!IsHost) return;
            if (!IsInitialized) _instance.Initialize();

            Recordables.Add(r);
        }

        public static void RemoveRecordable(Recordable r)
        {
            if (!IsHost) return;
            if (!IsInitialized) _instance.Initialize();

            Recordables.Remove(r);
        }
    }
}