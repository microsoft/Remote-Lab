using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RemoteLab
{
    public class UIEventsManager : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            SetupUIEvents();
        }
        private void SetupUIEvents()
        {
            InteractableUI[] allInteractableUI = GameObject.FindObjectsOfType<InteractableUI>();
            foreach (InteractableUI interactableUI in allInteractableUI)
            {
                if (interactableUI.interactableType == InteractableUITypes.Button)
                {
                    Button button = interactableUI.gameObject.GetComponent<Button>();
                    if (button != null)
                    {
                        button.onClick.AddListener(delegate
                        {
                            ReplayManager.Instance.WriteUIEventDataEntry("button", "click", interactableUI.gameObject, interactableUI.guidString);
                        });
                    }
                }
                else if (interactableUI.interactableType == InteractableUITypes.Toggle)
                {
                    Toggle toggle = interactableUI.gameObject.GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.onValueChanged.AddListener(delegate
                        {
                            ReplayManager.Instance.WriteUIEventDataEntry("toggle", toggle.isOn.ToString(), interactableUI.gameObject, interactableUI.guidString);
                        });
                    }
                }
                else if (interactableUI.interactableType == InteractableUITypes.Slider)
                {
                    Slider slider = interactableUI.gameObject.GetComponent<Slider>();
                    if (slider != null)
                    {
                        slider.onValueChanged.AddListener(delegate
                        {
                            ReplayManager.Instance.WriteUIEventDataEntry("slider", slider.value.ToString(), interactableUI.gameObject, interactableUI.guidString);
                        });
                    }
                }
            }
        }
    }
}