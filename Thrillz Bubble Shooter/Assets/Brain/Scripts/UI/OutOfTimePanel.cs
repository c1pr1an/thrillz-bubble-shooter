using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class OutOfTimePanel : MonoBehaviour
    {
        public void Display()
        {
            DOVirtual.DelayedCall(ScoreManager.SCORE_TEXT_DELAY, () =>
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
