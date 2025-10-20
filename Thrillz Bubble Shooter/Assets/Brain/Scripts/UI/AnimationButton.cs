using Brain.Managers;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Brain.UI
{
	[RequireComponent(typeof(Button))]
	public class AnimationButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
	{
		// Private Fields
		[NotNull]
		private Button _button;

		// Serialized Fields
		[Header("Animation References")]
		[SerializeField] private bool _useUnscaledTime = false;
		[SerializeField] private float _duration = 0.2f;
		[SerializeField] private Ease _ease = Ease.OutBack;
		[Range(0.0f, 1.0f)]
		[SerializeField] private float _scaleOnDown = 0.9f;
		[Tooltip("The transform in which the scale effect will be applied:\nIf left empty, it will use this object's transform")]
		[SerializeField] private Transform _targetTransform;

		// Unity Lifecycle
		private void Awake()
		{
			_button = GetComponent<Button>();

			if (!_targetTransform)
				_targetTransform = transform;
		}

		private void OnDestroy()
		{
			DOTween.Kill(this);
			KillTweens();
		}

		// Event Handlers
		public void OnPointerClick(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			HapticManager.Instance.TriggerHaptic(HapticType.LightImpact);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			_targetTransform.DOScale(Vector3.one * _scaleOnDown, _duration).SetEase(_ease).SetUpdate(_useUnscaledTime);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			// Do animations
			_targetTransform.DOScale(Vector3.one, _duration).SetEase(_ease).SetUpdate(_useUnscaledTime);
		}

		// Private Methods
		private void KillTweens()
		{
			DOTween.Kill(_targetTransform);
		}
	}
}