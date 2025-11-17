using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class LimitHitPanel : MonoBehaviour
    {
        // Public Methods
        public void Display()
        {
            DOVirtual.DelayedCall(0.25f, () =>
            {
                gameObject.SetActive(true);
                SoundManager.Instance.StopMusic();
            });
        }

        public void OnAnimationEnd()
        {
            //UIManager.Instance.GlobalPauseMode.EndGame(0f);
        }
    }
}
