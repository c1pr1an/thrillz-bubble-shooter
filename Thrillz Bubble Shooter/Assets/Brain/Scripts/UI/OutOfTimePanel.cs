using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class OutOfTimePanel : MonoBehaviour
    {
        // Public Methods
        public void Display()
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                gameObject.SetActive(true);
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_OutOfTime);
            });
        }

        public void OnAnimationEnd()
        {
            //UIManager.Instance.GlobalPauseMode.EndGame(0f);
        }
    }
}
