using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RemoteLab
{
    public class ReplayUIManager : MonoBehaviour
    {
        [SerializeField] Button playPauseButton;
        [SerializeField] Button stopButton;

        private void Start()
        {
            playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            stopButton.onClick.AddListener(OnStopClicked);
        }

        private void OnPlayPauseClicked()
        {
            ReplaySystem.Instance?.OnPlayPauseClick();
        }

        private void OnStopClicked()
        {
            ReplaySystem.Instance?.OnClickStop();
        }

        private void OnDestroy()
        {
            playPauseButton?.onClick.RemoveListener(OnPlayPauseClicked);
            stopButton?.onClick.RemoveListener(OnStopClicked);
        }
    }
}