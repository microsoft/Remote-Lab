using System;
using UnityEngine;
using UnityEngine.UI;

namespace RemoteLab
{
    public enum InteractableUITypes
    {
        Button,
        Toggle,
        Slider
    }

    public class InteractableUI : MonoBehaviour
    {
        public InteractableUITypes interactableType;
        private bool isInstanced;
        [HideInInspector] public string guidString;
        [HideInInspector] public bool registeredToManager;

        private void Awake()
        {
            if (guidString.Equals(""))
            {
                isInstanced = true;
                guidString = Guid.NewGuid().ToString();
            }
            else
            {
                isInstanced = false;
            }
        }

        void Start()
        {
            SetupEventHandler();
        }

        private void FixedUpdate()
        {
            if (ReplayManager.Instance == null)
                return;

            if (!registeredToManager)
            {
                ReplayManager.Instance.interactableUIs.Add(this);
                registeredToManager = true;
            }
        }

        public bool IsInstanced()
        {
            return isInstanced;
        }

        private void SetupEventHandler()
        {
            if (ReplayManager.Instance == null)
                return;

            if (interactableType == InteractableUITypes.Button)
            {
                Button button = GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(delegate
                    {
                        ReplayManager.Instance.WriteUIEventDataEntry("button", "click", gameObject, guidString);
                    });
                }
            }
            else if (interactableType == InteractableUITypes.Toggle)
            {
                Toggle toggle = GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(delegate
                    {
                        ReplayManager.Instance.WriteUIEventDataEntry("toggle", toggle.isOn.ToString(), gameObject, guidString);
                    });
                }
            }
            else if (interactableType == InteractableUITypes.Slider)
            {
                Slider slider = GetComponent<Slider>();
                if (slider != null)
                {
                    slider.onValueChanged.AddListener(delegate
                    {
                        ReplayManager.Instance.WriteUIEventDataEntry("slider", slider.value.ToString(), gameObject, guidString);
                    });
                }
            }
            else
            {
                print("Did not set up any handler for UI " + gameObject);
            }
        }

        public void WriteStateEntry()
        {
            if (ReplayManager.Instance == null || !ReplayManager.Instance.recording)
                return;

            if (interactableType == InteractableUITypes.Toggle)
            {
                Toggle toggle = GetComponent<Toggle>();
                if (toggle != null)
                {
                    ReplayManager.Instance?.WriteUIEventDataEntry("toggle", toggle.isOn.ToString(), gameObject, guidString);
                }
            }
            else if (interactableType == InteractableUITypes.Slider)
            {
                Slider slider = GetComponent<Slider>();
                if (slider != null)
                {
                    ReplayManager.Instance?.WriteUIEventDataEntry("slider", slider.value.ToString(), gameObject, guidString);
                }
            }
        }

        private void OnEnable()
        {
            if (ReplayManager.Instance == null)
                return;

            WriteStateEntry();
            ReplayManager.Instance.interactableUIs.Remove(this);
        }

        private void OnDisable()
        {
            WriteStateEntry();
            ReplayManager.Instance.interactableUIs.Remove(this);
        }

        private void OnDestroy()
        {
            if (ReplayManager.Instance == null)
                return;

            WriteStateEntry();
            ReplayManager.Instance.interactableUIs.Remove(this);
        }

        private void OnApplicationQuit()
        {
            if (ReplayManager.Instance == null)
                return;

            WriteStateEntry();
            ReplayManager.Instance.interactableUIs.Remove(this);
        }
    }
}
