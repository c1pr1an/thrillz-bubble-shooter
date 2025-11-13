using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Brain.Util;
using Brain.Managers;

namespace Brain.Gameplay.Containers
{
    public abstract class BallContainerBase : MonoBehaviour
    {
        [Header("Container Settings")]
        [SerializeField] protected Transform _ballHolder;

        [Header("Swap Animation")]
        [SerializeField] private Transform _circleCenter;
        [SerializeField] private float _swapDuration = 0.3f;

        protected Ball _savedBall;
        protected Coroutine _switchCoroutine;
        protected bool _isSwapping = false;

        public Ball CurrentBall { get; set; }
        public bool IsSwapping => _isSwapping;
        public bool HasBall => CurrentBall != null;

        protected virtual void Awake()
        {
            if (_ballHolder == null)
            {
                _ballHolder = transform;
            }
        }

        protected virtual void OnEnable()
        {
            // Subscribe to color exhausted event
            if (ColorTrackerManager.Instance != null)
            {
                ColorTrackerManager.Instance.OnColorExhausted += OnColorExhausted;
            }
        }

        protected virtual void OnDisable()
        {
            // Unsubscribe from color exhausted event
            if (ColorTrackerManager.Instance != null)
            {
                ColorTrackerManager.Instance.OnColorExhausted -= OnColorExhausted;
            }

            if (_switchCoroutine != null)
            {
                StopCoroutine(_switchCoroutine);
                _switchCoroutine = null;
                _isSwapping = false;
            }
        }

        protected virtual void OnBallReceived(Ball ball)
        {
        }

        protected virtual void OnBallReleased(Ball ball)
        {
        }

        protected virtual Ball SpawnBall(BallColor color)
        {
            GameObject ballObj = ObjectPooler.Instance.Get(color);
            Ball ball = ballObj.GetComponent<Ball>();

            if (ball == null)
            {
                Debug.LogError($"BallContainerBase: Pooled object doesn't have Ball component!");
                return null;
            }

            ball.transform.position = _ballHolder.position;
            ball.transform.rotation = Quaternion.identity;
            ball.transform.SetParent(_ballHolder);
            ball.SetColor(color);
            ball.name = $"{gameObject.name}_Ball_{color}";

            ball.Flags = BallFlags.None;
            ball.SetColliderEnabled(false);

            CurrentBall = ball;

            return ball;
        }

        protected virtual Ball SpawnRandomBall()
        {
            if (GridGenerator.Instance.IsInitialized == false)
            {
                int colorCount = System.Enum.GetValues(typeof(BallColor)).Length;
                BallColor randomColor = (BallColor)UnityEngine.Random.Range(0, colorCount);
                return SpawnBall(randomColor);
            }
            else
            {
                // Use ColorTracker to get a valid color
                if (ColorTrackerManager.Instance != null && ColorTrackerManager.Instance.TryGenerateColor(out BallColor color))
                {
                    return SpawnBall(color);
                }
            }
            // Fallback if ColorTracker not available or grid is empty
            return null;
        }

        public virtual void SaveBall()
        {
            if (CurrentBall != null)
            {
                _savedBall = CurrentBall;
                CurrentBall = null;
            }
        }

        public virtual void RestoreSavedBall()
        {
            if (_savedBall != null)
            {
                CurrentBall = _savedBall;
                _savedBall = null;

                if (CurrentBall != null)
                {
                    CurrentBall.transform.position = _ballHolder.position;
                    CurrentBall.transform.SetParent(_ballHolder);
                    OnBallReceived(CurrentBall);
                }
            }
        }

        public void SwitchBall(BallContainerBase from, BallContainerBase to, Ball ball, Action<Ball> onComplete = null)
        {
            if (_switchCoroutine != null || ball == null)
                return;

            _switchCoroutine = StartCoroutine(SwitchBallAnimation(from, to, ball, onComplete));
        }

        protected IEnumerator SwitchBallAnimation(BallContainerBase from, BallContainerBase to, Ball ball, Action<Ball> onComplete)
        {
            _isSwapping = true;
            ball.transform.SetParent(null);

            bool isFromBonusContainer = from is BonusBallContainer;

            if (isFromBonusContainer)
            {
                yield return AnimateBonusSwap(from, to, ball);
            }
            else
            {
                yield return AnimateNormalSwap(from, to, ball);
            }

            ball.transform.position = to._ballHolder.position;
            ball.transform.rotation = Quaternion.identity;
            ball.transform.SetParent(to._ballHolder);

            _switchCoroutine = null;
            _isSwapping = false;

            onComplete?.Invoke(ball);
        }

        private IEnumerator AnimateBonusSwap(BallContainerBase from, BallContainerBase to, Ball ball)
        {
            Vector3 startPos = from._ballHolder.position;
            Vector3 endPos = to._ballHolder.position;
            float duration = 0.25f;

            ApplyScaleEffects(ball, from, to);
            Tween moveTween = ball.transform.DOMove(endPos, duration).SetEase(Ease.InOutQuad);

            yield return moveTween.WaitForCompletion();
        }

        private IEnumerator AnimateNormalSwap(BallContainerBase from, BallContainerBase to, Ball ball)
        {
            Vector3 startPos = from._ballHolder.position;
            Vector3 endPos = to._ballHolder.position;
            Vector3 centerPos = _circleCenter != null ? _circleCenter.position : (startPos + endPos) * 0.5f;

            float radius = Vector3.Distance(centerPos, startPos);
            float duration = _swapDuration;
            float elapsedTime = 0f;

            float startAngle = GetAngle(startPos - centerPos);
            float endAngle = GetAngle(endPos - centerPos);
            endAngle = NormalizeAntiClockwise(startAngle, endAngle);

            ApplyScaleEffects(ball, from, to);

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float easedT = Mathf.SmoothStep(0, 1, t);
                float currentAngle = Mathf.Lerp(startAngle, endAngle, easedT);

                ball.transform.position = GetCircularPosition(centerPos, currentAngle, radius);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        private float GetAngle(Vector3 direction)
        {
            Vector3 normalized = direction.normalized;
            return Mathf.Atan2(normalized.y, normalized.x) * Mathf.Rad2Deg;
        }

        private float NormalizeAntiClockwise(float startAngle, float endAngle)
        {
            float angleDiff = endAngle - startAngle;

            while (angleDiff > 180f) angleDiff -= 360f;
            while (angleDiff < -180f) angleDiff += 360f;

            if (angleDiff < 0)
            {
                endAngle = startAngle + (360f + angleDiff);
            }

            return endAngle;
        }

        private Vector3 GetCircularPosition(Vector3 center, float angle, float radius)
        {
            float radians = angle * Mathf.Deg2Rad;
            return center + new Vector3(
                Mathf.Cos(radians) * radius,
                Mathf.Sin(radians) * radius,
                0
            );
        }

        private void ApplyScaleEffects(Ball ball, BallContainerBase from, BallContainerBase to)
        {
            if (to is LaunchContainer)
            {
                ball.AnimateScaleUp(0.3f, () =>
                {
                    ball.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
                });
            }
            else if (from is LaunchContainer)
            {
                ball.AnimateScaleDown();
            }
        }

        public void SwapBalls(BallContainerBase otherContainer)
        {
            if (!CanSwap(otherContainer))
                return;

            Ball myBall = CurrentBall;
            Ball otherBall = otherContainer.CurrentBall;

            CurrentBall = null;
            otherContainer.CurrentBall = null;

            OnBallReleased(myBall);
            otherContainer.OnBallReleased(otherBall);

            bool isFromBonusContainer = this is BonusBallContainer;

            // Set swapping state for the other container when coming from bonus
            if (isFromBonusContainer)
            {
                otherContainer._isSwapping = true;
            }

            SwitchBall(this, otherContainer, myBall, (ball) =>
            {
                otherContainer.CurrentBall = ball;
                otherContainer.OnBallReceived(ball);

                if (isFromBonusContainer)
                {
                    otherContainer._isSwapping = false;
                    if (otherBall != null) ObjectPooler.Instance.Release(otherBall.gameObject, otherBall.Color);
                }
            });

            // If not from bonus container, do the normal two-way swap
            if (!isFromBonusContainer)
            {
                otherContainer.SwitchBall(otherContainer, this, otherBall, (ball) =>
                {
                    CurrentBall = ball;
                    OnBallReceived(ball);
                });
            }
        }

        protected virtual bool CanSwap(BallContainerBase otherContainer)
        {
            return CurrentBall != null &&
                   otherContainer != null &&
                   !_isSwapping &&
                   !otherContainer._isSwapping;
        }

        public virtual void ReleaseBall()
        {
            if (CurrentBall != null)
            {
                Ball releasedBall = CurrentBall;
                CurrentBall = null;
                OnBallReleased(releasedBall);
            }
        }

        protected void SetBallParent(Ball ball)
        {
            if (ball != null && _ballHolder != null)
            {
                ball.transform.SetParent(_ballHolder);
                ball.transform.localPosition = Vector3.zero;
            }
        }

        /// <summary>
        /// Called when a color is exhausted from the grid - instantly replaces ball if needed
        /// </summary>
        protected virtual void OnColorExhausted(BallColor exhaustedColor)
        {
            // Only replace if we have a ball of the exhausted color and it's not a bonus ball
            if (CurrentBall != null &&
                !CurrentBall.IsBonusBall &&
                CurrentBall.Color == exhaustedColor)
            {
                // Instant replacement - no animation
                Destroy(CurrentBall.gameObject);
                CurrentBall = SpawnRandomBall();

                if (CurrentBall != null)
                {
                    CurrentBall.SetColliderEnabled(false);
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnDrawGizmosSelected()
        {
            if (_ballHolder != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_ballHolder.position, 0.3f);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }

            if (CurrentBall != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(CurrentBall.transform.position, 0.35f);
            }
        }
#endif
    }
}
