using System;
using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Managers;
using Brain.Util;
using DG.Tweening;
using UnityEngine;

namespace Brain.Gameplay.Containers
{
    public class LaunchContainer : BallContainerBase
    {
        public event Action<Ball> OnBallLaunched;

        [Header("Launch Settings")]
        [SerializeField] private float _minAimAngle;

        [Header("Trajectory")]
        [SerializeField] private TrajectoryPredictor _trajectoryPredictor;
        [SerializeField] private ParticleSystem _aimingVFX;
        [SerializeField] private Transform _ballAimHighlight;

        private BallPreviewContainer _previewContainer;
        private BonusBallContainer _bonusBallContainer;
        private Camera _mainCamera;
        private bool _canLaunch = true;
        private bool _waitingForBall = false;
        private BonusBallBase _currentBonusBallComponent;

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

        protected override void OnEnable()
        {
            base.OnEnable();

            // Subscribe to InputManager events
            InputManager.OnAimingStarted += OnAimingStarted;
            InputManager.OnAimingUpdated += OnAimingUpdated;
            InputManager.OnAimingReleased += OnAimingReleased;
            InputManager.OnAimingCancelled += OnAimingCancelled;
            ColorTrackerManager.Instance.OnColorExhausted += OnColorExhausted;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            // Unsubscribe from InputManager events
            InputManager.OnAimingStarted -= OnAimingStarted;
            InputManager.OnAimingUpdated -= OnAimingUpdated;
            InputManager.OnAimingReleased -= OnAimingReleased;
            InputManager.OnAimingCancelled -= OnAimingCancelled;
            ColorTrackerManager.Instance.OnColorExhausted -= OnColorExhausted;
        }

        public void Init(BallPreviewContainer previewContainer, BonusBallContainer bonusBallContainer = null)
        {
            _previewContainer = previewContainer;
            _bonusBallContainer = bonusBallContainer;
            _aimingVFX.Stop();
            _ballAimHighlight.gameObject.SetActive(false);
            if (_previewContainer.HasBall)
            {
                Ball previewBall = _previewContainer.CurrentBall;
                _previewContainer.ReleaseBall();

                previewBall.transform.SetParent(_ballHolder);
                previewBall.transform.position = _ballHolder.position;
                previewBall.AnimateScaleTo(1.2f, 0f);

                CurrentBall = previewBall;
                OnBallReceived(previewBall);
                _previewContainer.SpawnBall();
            }
        }

        private void Update()
        {
            if (CurrentBall == null && !_waitingForBall && CanLaunch)
            {
                PullFromPreview();
                return;
            }
        }

        private void OnAimingStarted(Vector2 position)
        {
            // Only start aiming if we can launch and game is active
            if (!CanLaunch || CurrentBall == null)
            {
                _trajectoryPredictor.HideTrajectory();
                return;
            }

            UpdateTrajectory(position);

            // Update rocket ball rotation if applicable
            UpdateRocketBallAiming(position, true);
        }

        private void OnAimingUpdated(Vector2 position)
        {
            // Continue showing trajectory while aiming and game is active
            if (!CanLaunch || CurrentBall == null)
            {
                _trajectoryPredictor.HideTrajectory();
                return;
            }

            UpdateTrajectory(position);

            // Update rocket ball rotation if applicable
            UpdateRocketBallAiming(position, true);
        }

        private void OnAimingReleased(Vector2 position)
        {
            // Launch the ball when input is released and game is active
            if (!CanLaunch || CurrentBall == null)
            {
                _trajectoryPredictor.HideTrajectory();
                return;
            }

            Vector2 aimDirection = CalculateAimDirection(position);
            float angle = Vector2.SignedAngle(Vector2.right, aimDirection);

            // Check if angle is valid before launching AND we are not hovering over a container
            if (!IsAngleValid(angle) || InputManager.Instance.IsPointerOverContainer(position))
            {
                OnAimingCancelled();
                return;
            }

            _aimingVFX.Stop();
            _ballAimHighlight.gameObject.SetActive(false);

            LaunchBall(aimDirection);
        }

        private void OnAimingCancelled()
        {
            // Hide trajectory when aiming is cancelled
            if (_trajectoryPredictor != null)
            {
                _trajectoryPredictor.HideTrajectory();
            }

            _aimingVFX.Stop();
            _ballAimHighlight.gameObject.SetActive(false);

            // Reset rocket ball rotation if applicable
            UpdateRocketBallAiming(Vector2.zero, false);
        }

        private void UpdateRocketBallAiming(Vector2 inputPosition, bool isAiming)
        {
            // Only update if we have a cached bonus ball component that's a rocket
            RocketBall rocketComponent = _currentBonusBallComponent as RocketBall;
            if (rocketComponent == null) return;

            if (isAiming)
            {
                Vector2 aimDirection = CalculateAimDirection(inputPosition);
                rocketComponent.SetAimDirection(aimDirection, true);
            }
            else
            {
                rocketComponent.SetAimDirection(Vector2.zero, false);
            }
        }

        private void UpdateTrajectory(Vector2 inputPosition)
        {
            if (_trajectoryPredictor == null || CurrentBall == null || !CanLaunch)
            {
                _trajectoryPredictor.HideTrajectory();
                return;
            }

            Vector2 aimDirection = CalculateAimDirection(inputPosition);
            float angle = Vector2.SignedAngle(Vector2.right, aimDirection);

            // Check if angle is valid AND we are not hovering over a container
            if (IsAngleValid(angle) && !InputManager.Instance.IsPointerOverContainer(inputPosition))
            {
                _trajectoryPredictor.ShowTrajectory(_ballHolder.position, aimDirection, CurrentBall);

                float vfxAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg - 90f;
                _aimingVFX.transform.rotation = Quaternion.Euler(0, 0, vfxAngle);

                if (!_aimingVFX.isPlaying)
                {
                    var mainModule = _aimingVFX.main;
                    mainModule.startColor = CurrentBall.DisplayColor;
                    _aimingVFX.Play();
                }
                if (CurrentBall.IsBonusBall == false) _ballAimHighlight.gameObject.SetActive(true);
            }
            else
            {
                _trajectoryPredictor.HideTrajectory();
                _aimingVFX.Stop();
                _ballAimHighlight.gameObject.SetActive(false);
            }
        }

        private Vector2 CalculateAimDirection(Vector2 inputPosition)
        {
            Vector2 aimDirection = (inputPosition - (Vector2)_ballHolder.position).normalized;
            return aimDirection;
        }

        private bool IsAngleValid(float angle)
        {
            // Angle is signed, so 0 is right, 90 is up, 180/-180 is left, -90 is down
            // We want 15 to 165 degrees
            return angle >= 15f && angle <= 165f;
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
                if (!ball.IsBonusBall &&
                    !ColorTrackerManager.Instance.IsColorAvailable(ball.Color))
                {
                    ball.ReturnToPool();
                    CurrentBall = SpawnRandomBall();
                    CurrentBall.AnimateScaleTo(1.2f, 0f);
                }

                ball.SetColliderEnabled(false);
                ball.Flags = BallFlags.None;

                _currentBonusBallComponent = ball.GetComponent<BonusBallBase>();

                RocketBall rocketBall = _currentBonusBallComponent as RocketBall;
                if (rocketBall != null)
                {
                    rocketBall.SetInLauncher(true);
                }
            }
        }

        public void OnColorExhausted(BallColor exhaustedColor)
        {
            // If current ball is of exhausted color and not a bonus ball, replace it
            if (CurrentBall != null &&
                !CurrentBall.IsBonusBall &&
                CurrentBall.Color == exhaustedColor)
            {
                CurrentBall.ReturnToPool();
                CurrentBall = SpawnRandomBall();
                CurrentBall.AnimateScaleTo(1.2f, 0f);
                OnBallReceived(CurrentBall);
            }
        }

        public override void ReleaseBall()
        {
            // Clear cached bonus ball component when releasing ball
            if (_currentBonusBallComponent != null)
            {
                // Only rocket balls have SetInLauncher method
                RocketBall rocketBall = _currentBonusBallComponent as RocketBall;
                if (rocketBall != null)
                {
                    rocketBall.SetInLauncher(false);
                }
                _currentBonusBallComponent = null;
            }

            base.ReleaseBall();
        }

        private void LaunchBall(Vector2 direction)
        {
            if (CurrentBall == null) return;

            // Double-check we can actually launch (safety check)
            if (!CanLaunch)
            {
                return;
            }

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
                // Rocket balls should follow exact trajectory without grid snapping
                if (CurrentBall != null && !CurrentBall.IsRocket())
                {
                    Vector3 originalEndpoint = trajectoryPath[trajectoryPath.Count - 1];
                    Vector3 snapPosition = GridManager.Instance.GetGridSnapPosition(originalEndpoint);
                    trajectoryPath[trajectoryPath.Count - 1] = snapPosition;

                    ClampTrajectoryToScreen(trajectoryPath);
                }
            }

            _canLaunch = false;

            if (_trajectoryPredictor != null)
            {
                _trajectoryPredictor.HideTrajectory();
            }

            Ball launchedBall = CurrentBall;

            // Save rocket component reference BEFORE releasing the ball
            RocketBall rocketComponent = _currentBonusBallComponent as RocketBall;

            ReleaseBall(); // This will set _currentBonusBallComponent to null

            launchedBall.transform.SetParent(null);
            launchedBall.AnimateScaleDown(0.15f);

            // Notify rocket ball that it's being launched (use saved reference)
            if (rocketComponent != null)
            {
                rocketComponent.SetFlying(true);
                // Set initial flight direction
                if (trajectoryPath != null && trajectoryPath.Count >= 2)
                {
                    Vector2 initialDirection = (trajectoryPath[1] - trajectoryPath[0]).normalized;
                    rocketComponent.UpdateFlightRotation(initialDirection, false); // false = not a bounce
                }
            }

            BallLaunch launcher = launchedBall.gameObject.AddComponent<BallLaunch>();
            launcher.OnBallStopped += (ball) => StartCoroutine(OnBallStopped(ball));
            launcher.LaunchAlongPath(trajectoryPath);

            OnBallLaunched?.Invoke(launchedBall);
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

                // Clamp Y to dynamic ceiling
                float ceilingY = UIManager.Instance.GameplayUI.GetCeilingY();
                // Ceiling is effective surface, so clamp center to ceiling - radius
                float maxY = Mathf.Min(vertExtent, ceilingY) - ballRadius;

                point.y = Mathf.Clamp(point.y, -vertExtent + ballRadius, maxY);

                trajectoryPath[i] = point;
            }
        }

        private IEnumerator OnBallStopped(Ball ball)
        {
            MatchDetector.Instance.ProcessBallStopped(ball);

            yield return new WaitWhile(() => DestroyManager.Instance.IsDestroying() ||
                                            OrphanDetector.Instance.IsChecking() ||
                                            OrphanDetector.Instance.IsAnimating() ||
                                            GridScrollManager.Instance.IsMoving);

            _canLaunch = true;
        }

        public bool CanLaunch => _canLaunch && !IsSwapping &&
                                  !DestroyManager.Instance.IsDestroying() &&
                                  !OrphanDetector.Instance.IsChecking() &&
                                  !OrphanDetector.Instance.IsAnimating() &&
                                  !GridScrollManager.Instance.IsMoving &&
                                  GameConditionsManager.Instance.IsGameActive;

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
