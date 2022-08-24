using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteLab
{
    [DisallowMultipleComponent]
    public class Recordable : MonoBehaviour
    {
        [HideInInspector] public bool isInstantiatedAtRuntime;
        [HideInInspector] public string resourceName;
        [HideInInspector] public string guidString;
        [HideInInspector] public bool loggedStart;
        [HideInInspector] public bool registeredToManager;

        private bool applicationQuit;
        private bool isInstanced;

        private void Awake()
        {
            isInstanced = isInstantiatedAtRuntime;

            if (guidString.Equals(""))
            {
                guidString = Guid.NewGuid().ToString();
            }
        }

        private void OnEnable()
        {
            if (guidString.Equals(""))
                guidString = Guid.NewGuid().ToString();

            if (loggedStart)
            {
                ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Activated, guidString, resourceName);
            }
            else
            {
                if (ReplayManager.Instance == null || !ReplayManager.Instance.recording)
                    return;

                ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Instantiated, guidString, resourceName);
                loggedStart = true;
            }
        }

        private void FixedUpdate()
        {
            if (ReplayManager.Instance == null || !ReplayManager.Instance.enabled)
                return;

            if (!registeredToManager)
            {
                ReplayManager.Instance.trackables.Add(this);
                registeredToManager = true;
            }

            if (transform.hasChanged)
            {
                if (loggedStart)
                {
                    ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Changed, guidString, resourceName);
                    transform.hasChanged = false;
                }
            }
        }

        private void OnDisable()
        {
            if (loggedStart)
            {
                ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Deactivated, guidString, resourceName);
            }
        }

        private void OnDestroy()
        {
            if (ReplayManager.Instance == null)
                return;

            // Create final entry for trackable (Destroyed)
            if (!applicationQuit)
            {
                ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Destroyed, guidString, resourceName);
                ReplayManager.Instance.trackables.Remove(this);
            }
        }

        private void OnApplicationQuit()
        {
            if (ReplayManager.Instance == null)
                return;

            applicationQuit = true;
            ReplayManager.Instance.WriteTransformDataEntry(transform, ReplayManager.ObjectStatus.Destroyed, guidString, resourceName);
            ReplayManager.Instance.trackables.Remove(this);
        }

        public bool IsInstanced()
        {
            return isInstanced;
        }
    }
}