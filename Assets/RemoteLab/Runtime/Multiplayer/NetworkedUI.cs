using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.EventSystems;

namespace RemoteLab
{
    [RequireComponent(typeof(PhotonView))]
    public class NetworkedUI : MonoBehaviour
    {
        [SerializeField] private InteractableUITypes interactableType;
        private PhotonView photonView;
        private Selectable uiElement;
        private PointerEventData pointer;
        private bool uiUpdated;
        private bool updatedByRPC;

        // Set up parameters
        private void Start()
        {
            photonView = GetComponent<PhotonView>();
            InitEventHandlers();
            updatedByRPC = uiUpdated = false;
        }

        public void InitEventHandlers()
        {
            if (interactableType == InteractableUITypes.Button)
            {
                Button button = GetComponent<Button>();
                print(button + " initializing");
                if (button != null)
                {
                    uiElement = button;
                    button.onClick.AddListener(BroadcastClick);
                }
            }
            else if (interactableType == InteractableUITypes.Toggle)
            {
                Toggle toggle = GetComponent<Toggle>();
                if (toggle != null)
                {
                    uiElement = toggle;
                    toggle.onValueChanged.AddListener(delegate
                    {
                        if (!uiUpdated && !updatedByRPC)
                        {
                            Debug.Log("Broadcasting toggle... " + uiElement);
                            photonView.RPC(nameof(OnToggle), RpcTarget.Others, toggle.isOn);
                            uiUpdated = true;
                            StartCoroutine(nameof(UiUpdateRoutine));
                        }

                        updatedByRPC = false;
                    });
                }
            }
            else if (interactableType == InteractableUITypes.Slider)
            {
                Slider slider = GetComponent<Slider>();
                if (slider != null)
                {
                    uiElement = slider;
                    slider.onValueChanged.AddListener(delegate
                    {
                        if (!uiUpdated && !updatedByRPC)
                        {
                            photonView.RPC(nameof(OnSlide), RpcTarget.Others, slider.value);
                            uiUpdated = true;
                            StartCoroutine(nameof(UiUpdateRoutine));
                        }

                        updatedByRPC = false;
                    });
                }
            }

            pointer = new PointerEventData(EventSystem.current);
        }

        public void BroadcastClick()
        {
            if (!uiUpdated && !updatedByRPC)
            {
                Debug.Log("Broadcasting click.." + uiElement);
                photonView.RPC(nameof(OnClick), RpcTarget.Others);
                uiUpdated = true;
                StartCoroutine(nameof(UiUpdateRoutine));
            }

            updatedByRPC = false;
        }

        IEnumerator UiUpdateRoutine()
        {
            yield return new WaitForSeconds(0.1f);
            uiUpdated = false;
        }

        // Photon RPC Method to broadcast the toggle
        [PunRPC]
        void OnToggle(bool value)
        {
            if (uiElement == null)
                return;

            updatedByRPC = true;
            ((Toggle)uiElement).isOn = value;
        }

        [PunRPC]
        void OnSlide(float value)
        {
            updatedByRPC = true;
            ((Slider)uiElement).value = value;
        }

        [PunRPC]
        void OnClick()
        {
            updatedByRPC = true;
            ExecuteEvents.Execute(gameObject, pointer, ExecuteEvents.pointerClickHandler);
        }

        public void SetRPCUpdated(bool status)
        {
            updatedByRPC = status;
        }

        void OnDisable()
        {
            uiUpdated = false;
        }
    }
}