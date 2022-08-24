using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RemoteLab
{
    public class ReplaySlider : MonoBehaviour
    {
        [SerializeField] private Slider slider;

        public void OnSliderChange()
        {
            ReplaySystem.Instance.OnSliderHit(slider.value);
        }

        private void Update()
        {
            float prop = ((float)(ReplaySystem.Instance.GetCurFrame())) / ReplaySystem.Instance.GetTotalFrames();
            slider.value = prop;
        }
    }
}