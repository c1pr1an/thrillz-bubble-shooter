using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Brain.Util
{
    public class Helper : MonoBehaviour
    {
        public static void FadeInCanvasGroup(CanvasGroup canvasGroup)
        {
            if (canvasGroup.gameObject.activeSelf && canvasGroup.alpha == 1f) return;
            canvasGroup.alpha = 0f;
            canvasGroup.gameObject.SetActive(true);
            canvasGroup.DOFade(1f, 0.4f);
        }

        public static void AddTagRecursively(Transform trans, string tag)
        {
            trans.gameObject.tag = tag;
            foreach (Transform t in trans)
                AddTagRecursively(t, tag);
        }

        static PointerEventData s_EventDataCurrentPosition;
        static List<RaycastResult> s_Results;
        public static bool IsPointerOverUIObject()
        {
            // Referencing this code for GraphicRaycaster https://gist.github.com/stramit/ead7ca1f432f3c0f181f
            // the ray cast appears to require only eventData.position.
            s_EventDataCurrentPosition = new PointerEventData(EventSystem.current);
            s_EventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            s_Results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(s_EventDataCurrentPosition, s_Results);
            return s_Results.Count > 0;
        }

        public static void CopyTransformData(Transform sourceTransform, Transform destinationTransform, Vector3 velocity)
        {
            if (sourceTransform.childCount != destinationTransform.childCount)
            {
                Debug.LogWarning("Invalid transform copy, they need to match transform hierarchies");
                return;
            }

            for (int i = 0; i < sourceTransform.childCount; i++)
            {
                var source = sourceTransform.GetChild(i);
                var destination = destinationTransform.GetChild(i);
                destination.position = source.position;
                destination.rotation = source.rotation;
                var rb = destination.GetComponent<Rigidbody>();
                if (rb != null) rb.velocity = velocity;

                CopyTransformData(source, destination, velocity);
            }
        }

        public static bool IndexInBounds(int index, int listLength)
        {
            return (index >= 0) && (index < listLength);
        }

        public static Vector2 WorldPositionToScreenSpaceCameraPosition(Camera worldCamera, Canvas canvas, Vector3 position)
        {
            Vector2 viewport = worldCamera.WorldToViewportPoint(position);
            Ray canvasRay = canvas.worldCamera.ViewportPointToRay(viewport);
            return canvasRay.GetPoint(canvas.planeDistance);
        }

        private static readonly Dictionary<float, WaitForSeconds> s_WaitDictionary = new Dictionary<float, WaitForSeconds>();
        public static WaitForSeconds GetWait(float time)
        {
            if (s_WaitDictionary.TryGetValue(time, out var wait)) return wait;

            s_WaitDictionary[time] = new WaitForSeconds(time);
            return s_WaitDictionary[time];
        }

        public static Vector3 GetVisualCenter(RectTransform rect)
        {
            Vector3 center = rect.position;
            center.x += rect.rect.width * (0.5f - rect.pivot.x) * rect.lossyScale.x;
            center.y += rect.rect.height * (0.5f - rect.pivot.y) * rect.lossyScale.y;
            return center;
        }
    }
}