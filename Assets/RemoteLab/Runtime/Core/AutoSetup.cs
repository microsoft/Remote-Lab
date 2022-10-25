using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEditor;
using UnityEngine.EventSystems;

namespace RemoteLab
{
    public class AutoSetup : MonoBehaviour
    {
        [SerializeField] GameObject replayUI;
        [SerializeField] GameObject replayCollection;

#if UNITY_EDITOR
        public void SetupReplay()
        {
            Debug.Log("Executing setup routine");

            // Delete PhotonComponents
            DeleteObjectsOfType<NetDistanceGrabbable>();
            DeleteObjectsOfType<PhotonTransformView>();
            DeleteObjectsOfType<PhotonView>();
            DeleteObjectsOfType<PhotonRigidbodyView>();
            DeleteObjectsOfType<NetTransfer>();
            DeleteObjectsOfType<NetworkManager>();
            DeleteObjectsOfType<OwnershipManager>();
            DeleteObjectsOfType<NetworkedUI>();

            RelinkPlayer();
            ReplaceReplayManager();

            SetupRigidbodies();
            SetupEventSystems();

            ReparentObjects();
            ReplaceQuestionnaires();

            AddReplayUI();
        }

        public void DeleteObjectsOfType<T>() where T : UnityEngine.Component
        {
            T[] toDelete = FindObjectsOfType<T>();

            foreach (T obj in toDelete)
            {
                if (typeof(T) == typeof(NetTransfer) || typeof(T) == typeof(NetworkManager))
                {
                    DestroyImmediate(obj.gameObject);
                }
                else
                {
                    DestroyImmediate(obj);
                }
            }
        }

        public void DisableObjectsOfType<T>() where T : MonoBehaviour
        {
            T[] toDelete = FindObjectsOfType<T>();

            foreach (T obj in toDelete)
            {
                obj.enabled = false;
            }
        }

        void ReplaceReplayManager()
        {
            ReplayManager replayManager = FindObjectOfType<ReplayManager>();
            replayManager.enabled = false;
            ReplaySystem replaySystem = replayManager.GetComponent<ReplaySystem>();
            replaySystem.enabled = true;
        }

        void ReplaceQuestionnaires()
        {
            NetQuestionnaireManager[] netQuestionnaires = FindObjectsOfType<NetQuestionnaireManager>();

            foreach (NetQuestionnaireManager netQuestionnaire in netQuestionnaires)
            {
                QuestionnaireManager sm = netQuestionnaire.GetComponent<QuestionnaireManager>();
                sm.enabled = true;
                netQuestionnaire.enabled = false;
            }
        }

        void RelinkPlayer()
        {
            DeleteObjectsOfType<NetVRPlayer>();
            DeleteObjectsOfType<NetHand>();
            DeleteObjectsOfType<NetDistanceGrabber>();

            // Disable OVR scripts
            DeleteObjectsOfType<OVRPhysicsRaycaster>();
            DeleteObjectsOfType<CharacterCameraConstraint>();
            DeleteObjectsOfType<SimpleCapsuleWithStickMovement>();
            DeleteObjectsOfType<OVRCameraRig>();
            DeleteObjectsOfType<OVRManager>();
            DeleteObjectsOfType<OVRScreenFade>();

            GameObject locomotionController = FindObjectOfType<LocomotionController>()?.gameObject;

            if (locomotionController == null)
                return;

            foreach (var component in locomotionController.GetComponents<Component>())
            {
                if (!(component is Transform))
                {
                    DestroyImmediate(component);
                }
            }
        }

        void SetupRigidbodies()
        {
            Rigidbody[] rBodies = FindObjectsOfType<Rigidbody>();

            foreach (Rigidbody rBody in rBodies)
            {
                rBody.isKinematic = true;
            }
        }

        void SetupEventSystems()
        {
            HandedInputSelector handedInputSelector = FindObjectOfType<HandedInputSelector>();

            if (!handedInputSelector)
                return;

            GameObject eSystemObj = handedInputSelector.gameObject;
            handedInputSelector.enabled = false;
            eSystemObj?.transform.root.gameObject.SetActive(false);
        }

        void ReparentObjects()
        {
            if (replayCollection == null)
            {
                replayCollection = new GameObject("ReplayCollection");
                LocateAndReparentAllObjects();
            }
        }

        void LocateAndReparentAllObjects()
        {
            List<GameObject> objects = GetAllObjectsInScene();

            foreach (GameObject go in objects)
            {
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) &&
                    !(go.hideFlags == HideFlags.NotEditable ||
                    go.hideFlags == HideFlags.HideAndDontSave))
                {
                    if (go.transform.parent == null)
                    {
                        // Put into collection object
                        if (!go.GetComponent<ReplayManager>())
                        {
                            go.transform.SetParent(replayCollection.transform);
                        }
                    }
                }
            }
        }

        List<GameObject> GetAllObjectsInScene()
        {
            List<GameObject> objectsInScene = new List<GameObject>();

            foreach (GameObject go in Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])
            {
                if (!EditorUtility.IsPersistent(go.transform.root.gameObject) && (go.hideFlags == HideFlags.None))
                    objectsInScene.Add(go);
            }

            return objectsInScene;
        }

        void AddReplayUI()
        {
            if (!FindObjectOfType<ReplayUIManager>())
                Instantiate(replayUI);
        }
#endif
    }
}