using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class OutOfTimePanel : MonoBehaviour
    {
        [SerializeField] private Transform panelTransform;
        [SerializeField] private Transform textTransform;
        // Public Methods
        public void Display()
        {
            DOVirtual.DelayedCall(1f, () =>
            {
                panelTransform.localScale = 0.8f * Vector3.one;
                panelTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                textTransform.DORotate(new Vector3(0, 0, Random.Range(-5f, 5f)), 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(2, LoopType.Yoyo);
                gameObject.SetActive(true);
                SoundManager.Instance.StopMusic();
                SoundManager.Instance.PlaySfxOneShot(SoundType.UI_OutOfTime);
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
