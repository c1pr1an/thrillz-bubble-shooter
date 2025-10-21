using System.Collections.Generic;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    public class LaunchContainer : MonoBehaviour
    {
        // Private Fields
        [Header("Settings")]
        [SerializeField] private Transform _ballSpawnPoint;

        [Header("Aiming")]
        [SerializeField] private float _minAimAngle = 10f; // Min angle from horizontal (in degrees)
        [SerializeField] private float _maxAimAngle = 170f; // Max angle from horizontal (in degrees)

        [Header("Trajectory")]
        [SerializeField] private TrajectoryPredictor _trajectoryPredictor;

        private Ball _currentBall;
        private Camera _mainCamera;
        private bool _canLaunch = true;

        private void Awake()
        {
            _mainCamera = Camera.main;

            // Setup trajectory predictor
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
            // Spawn initial ball
            SpawnNewBall();
        }

        private void Update()
        {
            if (!_canLaunch || _currentBall == null) return;

            // Get mouse world position
            Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Calculate aim direction
            Vector2 aimDirection = (mousePos - transform.position).normalized;

            // Clamp aim angle (prevent shooting down or too horizontal)
            float angle = Vector2.SignedAngle(Vector2.right, aimDirection);
            angle = Mathf.Clamp(angle, _minAimAngle, _maxAimAngle);
            aimDirection = Quaternion.Euler(0, 0, angle) * Vector2.right;

            // Show trajectory
            if (_trajectoryPredictor != null)
            {
                _trajectoryPredictor.ShowTrajectory(_ballSpawnPoint.position, aimDirection);
            }

            // Launch on click
            if (Input.GetMouseButtonDown(0))
            {
                LaunchBall(aimDirection);
            }
        }

        private void SpawnNewBall()
        {
            if (_ballSpawnPoint == null)
            {
                Debug.LogError("LaunchContainer: Ball spawn point not assigned!");
                return;
            }

            // Pick a random color
            BallColor randomColor = (BallColor)Random.Range(0, 6);

            // Get prefab from GridManager
            Ball prefab = GridManager.Instance.GetBallPrefab(randomColor);
            if (prefab == null)
            {
                Debug.LogError($"LaunchContainer: Ball prefab for color {randomColor} not found in GridManager!");
                return;
            }

            // Instantiate the correct ball prefab
            _currentBall = Instantiate(prefab, _ballSpawnPoint.position, Quaternion.identity, _ballSpawnPoint);
            _currentBall.SetColor(randomColor);
            _currentBall.name = $"LaunchBall_{randomColor}";

            // Disable collider until launched
            _currentBall.SetColliderEnabled(false);
        }

        private void LaunchBall(Vector2 direction)
        {
            if (_currentBall == null) return;

            // Get the full trajectory path from the predictor
            List<Vector3> trajectoryPath = null;
            if (_trajectoryPredictor != null)
            {
                trajectoryPath = _trajectoryPredictor.CalculateTrajectory(_ballSpawnPoint.position, direction);
            }

            // Fallback if no trajectory calculated
            if (trajectoryPath == null || trajectoryPath.Count < 2)
            {
                // Create simple straight path if trajectory failed
                trajectoryPath = new List<Vector3>
                {
                    _ballSpawnPoint.position,
                    _ballSpawnPoint.position + (Vector3)(direction.normalized * 10f)
                };
            }

            // Adjust the trajectory endpoint to be the exact grid snap position
            if (trajectoryPath.Count > 0)
            {
                Vector3 originalEndpoint = trajectoryPath[trajectoryPath.Count - 1];

                // Get the grid snap position from GridManager
                Vector3 snapPosition = GridManager.Instance.GetGridSnapPosition(originalEndpoint);

                // Replace the last point with the snap position
                trajectoryPath[trajectoryPath.Count - 1] = snapPosition;

                // Also clamp all trajectory points to stay within screen bounds
                ClampTrajectoryToScreen(trajectoryPath);
            }

            // Disable launching until ball stops
            _canLaunch = false;

            // Hide trajectory
            if (_trajectoryPredictor != null)
            {
                _trajectoryPredictor.HideTrajectory();
            }

            // Unparent ball from spawn point
            _currentBall.transform.SetParent(null);

            // Add launch component and launch along the path
            BallLaunch launcher = _currentBall.gameObject.AddComponent<BallLaunch>();
            launcher.OnBallStopped += OnBallStopped;
            launcher.LaunchAlongPath(trajectoryPath);

            // Clear current ball reference
            _currentBall = null;
        }

        private void ClampTrajectoryToScreen(List<Vector3> trajectoryPath)
        {
            if (_mainCamera == null) return;

            float vertExtent = _mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            float ballRadius = 0.35f; // Ball radius to keep ball fully on screen

            for (int i = 0; i < trajectoryPath.Count; i++)
            {
                Vector3 point = trajectoryPath[i];

                // Clamp X to screen bounds
                point.x = Mathf.Clamp(point.x, -horzExtent + ballRadius, horzExtent - ballRadius);

                // Clamp Y to screen bounds (don't go below launcher area)
                point.y = Mathf.Clamp(point.y, -vertExtent + ballRadius, vertExtent - ballRadius);

                trajectoryPath[i] = point;
            }
        }

        private void OnBallStopped(Ball ball)
        {
            // Process match detection and orphan detection
            MatchingManager.Instance.ProcessBallStopped(ball);

            // Spawn next ball
            SpawnNewBall();

            // Re-enable launching
            _canLaunch = true;
        }

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
