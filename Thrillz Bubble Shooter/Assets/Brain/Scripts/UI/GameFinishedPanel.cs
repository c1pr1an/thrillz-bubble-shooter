using System.Collections;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class GameFinishedPanel : MonoBehaviour
    {
        [SerializeField] private Transform panelTransform;
        // Public Methods
        public void Display(float delay)
        {
            DOVirtual.DelayedCall(delay, () =>
            {
                panelTransform.localScale = 0.8f * Vector3.one;
                panelTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                gameObject.SetActive(true);
                SoundManager.Instance.StopMusic();
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_ConfettiPop);
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_GameFinished);
                Invoke(nameof(OnAnimationEnd), 2f);
            });
        }

        public void OnAnimationEnd()
        {
            GameController.Instance.RestartGame();
            //UIManager.Instance.GlobalPauseMode.EndGame(0f);
        }
    }
}
