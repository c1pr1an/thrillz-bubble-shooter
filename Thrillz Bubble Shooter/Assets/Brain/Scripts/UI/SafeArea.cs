using UnityEngine;

namespace Brain.UI
{
	public class SafeArea : MonoBehaviour
	{
		void Awake()
		{
			ApplySafeAreaToTransform();
		}

		private void ApplySafeAreaToTransform()
		{
			Rect safeArea = Screen.safeArea;
			Vector2 minAnchor = safeArea.position;
			Vector2 maxAnchor = safeArea.position + safeArea.size;

			minAnchor.x /= Screen.width;
			maxAnchor.x /= Screen.width;
			minAnchor.y /= Screen.height;
			maxAnchor.y /= Screen.height;

			RectTransform rectTransform = (RectTransform)transform;
			rectTransform.anchorMin = minAnchor;
			rectTransform.anchorMax = maxAnchor;
		}
	}
}