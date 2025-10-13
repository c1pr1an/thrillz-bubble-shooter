using Brain.Managers;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Brain.UI
{
	/// <summary>
	/// A button that animates its scale on pointer down and up events.
	/// </summary>
	[RequireComponent(typeof(Button))]
	public class AnimationButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
	{
		[NotNull]
		private Button _button;
		[Header("Animation References")]
		[SerializeField] private bool useUnscaledTime = false;
		[SerializeField] private float duration = 0.2f;
		[SerializeField] private Ease ease = Ease.OutBack;
		[Range(0.0f, 1.0f)]
		[SerializeField] private float scaleOnDown = 0.9f;
		[Tooltip("The transform in which the scale effect will be applied:\nIf left empty, it will use this object's transform")]
		[SerializeField] private Transform targetTransform;

		private void Awake()
		{
			_button = GetComponent<Button>();

			if (!targetTransform)
				targetTransform = transform;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			HapticManager.Instance.TriggerHaptic(HapticType.LightImpact);
		}

		public void OnPointerDown(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			targetTransform.DOScale(Vector3.one * scaleOnDown, duration).SetEase(ease).SetUpdate(useUnscaledTime);
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			if (!_button.interactable && _button != null) return;

			// Do animations
			targetTransform.DOScale(Vector3.one, duration).SetEase(ease).SetUpdate(useUnscaledTime);
		}

		private void KillTweens() //Prevent DOTween errors log when the object is destroyed
		{
			DOTween.Kill(targetTransform);
		}

		private void OnDestroy()
		{
			DOTween.Kill(this);
			KillTweens();
		}
	}
}