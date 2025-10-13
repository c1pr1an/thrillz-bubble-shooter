using Brain.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Brain.Audio
{
    [RequireComponent(typeof(Button))]
    public class ButtonTapFeedback : MonoBehaviour
    {
        public SoundType SoundType = SoundType.UI_TapButton;
        private Vector3 _startingScale;
        private Button _myButton;

        void OnEnable()
        {
            _myButton = GetComponent<Button>();
            _myButton.onClick.AddListener(OnButtonClick);
        }

        void OnDisable()
        {
            _myButton.onClick.RemoveListener(OnButtonClick);
        }

        private void OnButtonClick()
        {
            // Disable the button interactivity to prevent further clicks
            _myButton.interactable = false;

            if (_startingScale == Vector3.zero)
                _startingScale = transform.localScale;

            SoundManager.Instance.PlaySfxOneShot(SoundType);

            // Kill any previous animation on this button
            DOTween.Kill("ButtonTapFeedback" + GetInstanceID());
            transform.localScale = _startingScale;

            // Execute the punch scale animation and re-enable the button after it finishes
            transform.DOPunchScale(0.05f * Vector3.one, 0.2f, 0, 0f)
                .SetId("ButtonTapFeedback" + GetInstanceID())
                .OnComplete(() =>
                {
                    _myButton.interactable = true;
                });
        }
    }
}
