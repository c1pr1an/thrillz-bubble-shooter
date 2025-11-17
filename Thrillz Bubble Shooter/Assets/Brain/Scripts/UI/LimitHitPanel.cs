using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;

namespace Brain.UI
{
    public class LimitHitPanel : MonoBehaviour
    {
        [SerializeField] private Transform panelTransform;
        [SerializeField] private Transform textTransform;
        // Public Methods
        public void Display()
        {
            DOVirtual.DelayedCall(0.25f, () =>
            {
                panelTransform.localScale = 0.8f * Vector3.one;
                panelTransform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                // Rotate sequence: left, right, left, right, center
                textTransform.rotation = Quaternion.identity;
                Sequence rotationSequence = DOTween.Sequence();
                rotationSequence.Append(textTransform.DORotate(new Vector3(0, 0, -3f), 0.1f).SetEase(Ease.InOutSine))
                    .Append(textTransform.DORotate(new Vector3(0, 0, 3f), 0.2f).SetEase(Ease.InOutSine))
                    .Append(textTransform.DORotate(new Vector3(0, 0, -3f), 0.2f).SetEase(Ease.InOutSine))
                    .Append(textTransform.DORotate(new Vector3(0, 0, 3f), 0.2f).SetEase(Ease.InOutSine))
                    .Append(textTransform.DORotate(new Vector3(0, 0, 0f), 0.1f).SetEase(Ease.InOutSine));
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
