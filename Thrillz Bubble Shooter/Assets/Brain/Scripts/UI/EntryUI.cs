using System.Collections;
using System.Collections.Generic;
using Brain.Audio;
using Brain.Gameplay;
using Brain.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Brain.UI
{
    public class EntryUI : MonoBehaviour
    {

        [SerializeField] private Image _blackOverlayImage;
        [SerializeField] private RectTransform _readyText;
        [SerializeField] private RectTransform _goText;
        public void AnimateEntry()
        {
            _blackOverlayImage.color = new Color(0f, 0f, 0f, 1f);
            _blackOverlayImage
                .DOFade(0.3f, 0.15f)
                .SetEase(Ease.OutSine)
                .SetDelay(0.25f)
                .OnComplete(ShowReadyText);
        }

        private void ShowReadyText()
        {
            _readyText.anchoredPosition = new Vector2(-1000f, _readyText.anchoredPosition.y);
            _readyText.gameObject.SetActive(true);

            // Animate Ready text sliding in
            _readyText
                .DOAnchorPosX(0f, 0.5f)
                .SetEase(Ease.OutBack)
                .SetDelay(0.3f)
                .OnStart(() =>
                {
                    SoundManager.Instance.PlaySfxOneShot(SoundType.UI_ReadySignal);
                })
                .OnComplete(() =>
                {
                    GridGenerator.Instance.PlayEntryAnimation();
                });


            // Animate Ready text sliding out after delay
            _readyText
                .DOAnchorPosX(1000f, 0.5f)
                    .SetDelay(2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(ShowGoText);
        }

        private void ShowGoText()
        {
            _readyText.gameObject.SetActive(false);
            _goText.localScale = Vector3.zero;
            _goText.gameObject.SetActive(true);
            SoundManager.Instance.PlaySfxOneShot(SoundType.UI_GoSignal);

            _goText
                .DOScale(Vector3.one, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(FadeOutAndFinish);
        }

        private void FadeOutAndFinish()
        {
            // Fade out black overlay
            _blackOverlayImage
                .DOFade(0f, 0.15f)
                .SetDelay(0.5f)
                .SetEase(Ease.OutSine)
                .OnComplete(() => _blackOverlayImage.gameObject.SetActive(false));

            // Scale down Go text and start game
            _goText
                .DOScale(Vector3.zero, 0.3f)
                .SetDelay(0.5f)
                .SetEase(Ease.InBack)
                .OnComplete(() =>
                {
                    GameConditionsManager.Instance.StartGameTimer();
                    _goText.gameObject.SetActive(false);
                });
        }
    }
}
