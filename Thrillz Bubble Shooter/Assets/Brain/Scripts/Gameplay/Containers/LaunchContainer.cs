using System.Collections;
using System.Collections.Generic;
using Brain.Managers;
using Brain.Util;
using UnityEngine;

namespace Brain.Gameplay.Containers
{
    public class LaunchContainer : BallContainerBase
    {
        [Header("Launch Settings")]
        [SerializeField] private float _minAimAngle;

        [Header("Trajectory")]
        [SerializeField] private TrajectoryPredictor _trajectoryPredictor;

        [Header("References")]
        [SerializeField] private BallPreviewContainer _previewContainer;

        private Camera _mainCamera;
        private bool _canLaunch = true;
        private bool _waitingForBall = false;

        protected override void Awake()
        {
            base.Awake();
            _mainCamera = Camera.main;

            if (_trajectoryPredictor == null)
            {
                _trajectoryPredictor = GetComponent<TrajectoryPredictor>();
                if (_trajectoryPredictor == null)
                {
                    Debug.LogWarning("LaunchContainer: No TrajectoryPredictor found. Please add one or assign it in the Inspector.");
                }
            }
        }

        private void Start()
        {
        }

        private void Update()
        {
            if (CurrentBall == null && !_waitingForBall && _canLaunch)
            {
                PullFromPreview();
                return;
            }

            if (!_canLaunch || CurrentBall == null) return;

            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            Vector2 aimDirection = (mousePos - transform.position).normalized;

            float angle = Vector2.SignedAngle(Vector2.right, aimDirection);
            angle = Mathf.Clamp(angle, _minAimAngle, 180 - _minAimAngle);
            aimDirection = Quaternion.Euler(0, 0, angle) * Vector2.right;

            if (_trajectoryPredictor != null && CurrentBall != null)
            {
                _trajectoryPredictor.ShowTrajectory(_ballHolder.position, aimDirection, CurrentBall);
            }

            if (Input.GetMouseButtonDown(0))
            {
                LaunchBall(aimDirection);
            }
        }

        private void PullFromPreview()
        {
            if (_previewContainer == null || !_previewContainer.HasBall || _waitingForBall)
                return;

            _waitingForBall = true;

            Ball previewBall = _previewContainer.CurrentBall;
            _previewContainer.ReleaseBall();

            SwitchBall(_previewContainer, this, previewBall, (ball) =>
            {
                CurrentBall = ball;
                OnBallReceived(ball);
                _waitingForBall = false;
            });
        }

        protected override void OnBallReceived(Ball ball)
        {
            base.OnBallReceived(ball);

            if (ball != null)
            {
                ball.SetColliderEnabled(false);
                ball.Flags = BallFlags.None;
            }
        }

        private void LaunchBall(Vector2 direction)
        {
            if (CurrentBall == null) return;

            List<Vector3> trajectoryPath = null;
            if (_trajectoryPredictor != null)
            {
                trajectoryPath = _trajectoryPredictor.CalculateTrajectory(_ballHolder.position, direction);
            }

            if (trajectoryPath == null || trajectoryPath.Count < 2)
            {
                trajectoryPath = new List<Vector3>
                {
                    _ballHolder.position,
                    _ballHolder.position + (Vector3)(direction.normalized * 10f)
                };
            }

            if (trajectoryPath.Count > 0)
            {
                Vector3 originalEndpoint = trajectoryPath[trajectoryPath.Count - 1];
                Vector3 snapPosition = GridManager.Instance.GetGridSnapPosition(originalEndpoint);
                trajectoryPath[trajectoryPath.Count - 1] = snapPosition;

                ClampTrajectoryToScreen(trajectoryPath);
            }

            _canLaunch = false;

            if (_trajectoryPredictor != null)
            {
                _trajectoryPredictor.HideTrajectory();
            }

            Ball launchedBall = CurrentBall;
            ReleaseBall();

            launchedBall.transform.SetParent(null);

            BallLaunch launcher = launchedBall.gameObject.AddComponent<BallLaunch>();
            launcher.OnBallStopped += (ball) => StartCoroutine(OnBallStopped(ball));
            launcher.LaunchAlongPath(trajectoryPath);

            FireBallLaunchedEvent(launchedBall);
        }

        private void ClampTrajectoryToScreen(List<Vector3> trajectoryPath)
        {
            if (_mainCamera == null) return;

            float vertExtent = _mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            float ballRadius = 0.35f;

            for (int i = 0; i < trajectoryPath.Count; i++)
            {
                Vector3 point = trajectoryPath[i];

                point.x = Mathf.Clamp(point.x, -horzExtent + ballRadius, horzExtent - ballRadius);
                point.y = Mathf.Clamp(point.y, -vertExtent + ballRadius, vertExtent - ballRadius);

                trajectoryPath[i] = point;
            }
        }

        private IEnumerator OnBallStopped(Ball ball)
        {
            MatchDetector.Instance.ProcessBallStopped(ball);

            yield return new WaitWhile(() => DestroyManager.Instance.IsDestroying() ||
                                            OrphanDetector.Instance.IsChecking());

            _canLaunch = true;
        }

        public bool CanLaunch => _canLaunch && !IsSwapping;

        public void SetEnabled(bool enabled)
        {
            _canLaunch = enabled;

            if (!enabled && _trajectoryPredictor != null)
            {
                _trajectoryPredictor.HideTrajectory();
            }
        }
    }
}
