using System.Collections;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class GameFinishedPanel : MonoBehaviour
    {
        public void Display(float delay)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                gameObject.SetActive(true);
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_ConfettiPop);
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_GameFinished);
            });
        }

        public void OnAnimationEnd()
        {
            //UIManager.Instance.GlobalPauseMode.EndGame(0f);
        }
    }
}
