using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Brain.Util;

namespace Brain.Gameplay.Containers
{
    public abstract class BallContainerBase : MonoBehaviour
    {
        public static event Action<Ball> OnBallLaunched;
        public static event Action<Ball> OnBallSwitched;
        public static event Action<Ball> OnBallSpawned;

        [Header("Container Settings")]
        [SerializeField] protected Transform _ballHolder;
        protected Ball _savedBall;
        protected Coroutine _switchCoroutine;
        protected bool _isSwapping = false;

        public Ball CurrentBall { get; protected set; }
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
        }

        protected virtual void OnDisable()
        {
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
            OnBallSpawned?.Invoke(ball);

            return ball;
        }

        protected virtual Ball SpawnRandomBall()
        {
            int colorCount = System.Enum.GetValues(typeof(BallColor)).Length;
            BallColor randomColor = (BallColor)UnityEngine.Random.Range(0, colorCount);
            return SpawnBall(randomColor);
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

            float duration = 0.3f;
            float arcHeight = 2f;
            float elapsedTime = 0f;

            ball.transform.SetParent(null);

            Vector3 startPos = from._ballHolder.position;
            Vector3 endPos = to._ballHolder.position;
            Vector3 arcPeak = startPos + (endPos - startPos) * 0.5f + Vector3.up * arcHeight;

            Quaternion startRot = ball.transform.rotation;
            Quaternion targetRot = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-180f, 180f));

            // Determine if we need to scale up or down based on destination
            bool isGoingToLaunchContainer = to is LaunchContainer;
            bool isLeavingLaunchContainer = from is LaunchContainer;

            if (isGoingToLaunchContainer)
            {
                ball.AnimateScaleUp();
            }
            else if (isLeavingLaunchContainer)
            {
                ball.AnimateScaleDown();
            }

            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;

                Vector3 m0 = Vector3.Lerp(startPos, arcPeak, t);
                Vector3 m1 = Vector3.Lerp(arcPeak, endPos, t);
                Vector3 position = Vector3.Lerp(m0, m1, t);

                ball.transform.position = position;
                ball.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            ball.transform.position = endPos;
            ball.transform.rotation = Quaternion.identity;
            ball.transform.SetParent(to._ballHolder);

            _switchCoroutine = null;
            _isSwapping = false;

            onComplete?.Invoke(ball);
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

            SwitchBall(this, otherContainer, myBall, (ball) =>
            {
                otherContainer.CurrentBall = ball;
                otherContainer.OnBallReceived(ball);
            });

            otherContainer.SwitchBall(otherContainer, this, otherBall, (ball) =>
            {
                CurrentBall = ball;
                OnBallReceived(ball);
                OnBallSwitched?.Invoke(ball);
            });
        }

        protected virtual bool CanSwap(BallContainerBase otherContainer)
        {
            return CurrentBall != null &&
                   otherContainer != null &&
                   otherContainer.CurrentBall != null &&
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

        protected void FireBallLaunchedEvent(Ball ball)
        {
            OnBallLaunched?.Invoke(ball);
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
