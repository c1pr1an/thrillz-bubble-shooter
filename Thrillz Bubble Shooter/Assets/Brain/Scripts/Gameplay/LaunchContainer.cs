using System.Collections.Generic;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Manages the shooter at the bottom of the screen
    /// Spawns balls, handles aiming, and launches on click
    /// Simplified version inspired by BubbleShooterGameToolkit
    /// </summary>
    public class LaunchContainer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform ballSpawnPoint;

        [Header("Aiming")]
        [SerializeField] private float minAimAngle = 10f; // Min angle from horizontal (in degrees)
        [SerializeField] private float maxAimAngle = 170f; // Max angle from horizontal (in degrees)

        [Header("Trajectory")]
        [SerializeField] private TrajectoryPredictor trajectoryPredictor;

        private Ball currentBall;
        private Camera mainCamera;
        private bool canLaunch = true;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Setup trajectory predictor
            if (trajectoryPredictor == null)
            {
                trajectoryPredictor = GetComponent<TrajectoryPredictor>();
                if (trajectoryPredictor == null)
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
            if (!canLaunch || currentBall == null) return;

            // Get mouse world position
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // Calculate aim direction
            Vector2 aimDirection = (mousePos - transform.position).normalized;

            // Clamp aim angle (prevent shooting down or too horizontal)
            float angle = Vector2.SignedAngle(Vector2.right, aimDirection);
            angle = Mathf.Clamp(angle, minAimAngle, maxAimAngle);
            aimDirection = Quaternion.Euler(0, 0, angle) * Vector2.right;

            // Show trajectory
            if (trajectoryPredictor != null)
            {
                trajectoryPredictor.ShowTrajectory(ballSpawnPoint.position, aimDirection);
            }

            // Launch on click
            if (Input.GetMouseButtonDown(0))
            {
                LaunchBall(aimDirection);
            }
        }

        /// <summary>
        /// Spawns a new ball at the launch position
        /// </summary>
        private void SpawnNewBall()
        {
            if (ballSpawnPoint == null)
            {
                Debug.LogError("LaunchContainer: Ball spawn point not assigned!");
                return;
            }

            // Pick a random color
            BallColor randomColor = (BallColor)Random.Range(0, 6);
            int colorIndex = (int)randomColor;

            // Get prefab from GridManager
            Ball prefab = GridManager.Instance.GetBallPrefab(colorIndex);
            if (prefab == null)
            {
                Debug.LogError($"LaunchContainer: Ball prefab for color {randomColor} (index {colorIndex}) not found in GridManager!");
                return;
            }

            // Instantiate the correct ball prefab
            currentBall = Instantiate(prefab, ballSpawnPoint.position, Quaternion.identity, ballSpawnPoint);
            currentBall.SetColor(randomColor);
            currentBall.name = $"LaunchBall_{randomColor}";

            // Disable collider until launched
            currentBall.SetColliderEnabled(false);
        }

        /// <summary>
        /// Launches the current ball in the given direction
        /// </summary>
        private void LaunchBall(Vector2 direction)
        {
            if (currentBall == null) return;

            // Get the full trajectory path from the predictor
            List<Vector3> trajectoryPath = null;
            if (trajectoryPredictor != null)
            {
                trajectoryPath = trajectoryPredictor.CalculateTrajectory(ballSpawnPoint.position, direction);
            }

            // Fallback if no trajectory calculated
            if (trajectoryPath == null || trajectoryPath.Count < 2)
            {
                // Create simple straight path if trajectory failed
                trajectoryPath = new List<Vector3>
                {
                    ballSpawnPoint.position,
                    ballSpawnPoint.position + (Vector3)(direction.normalized * 10f)
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
            canLaunch = false;

            // Hide trajectory
            if (trajectoryPredictor != null)
            {
                trajectoryPredictor.HideTrajectory();
            }

            // Unparent ball from spawn point
            currentBall.transform.SetParent(null);

            // Add launch component and launch along the path
            BallLaunch launcher = currentBall.gameObject.AddComponent<BallLaunch>();
            launcher.OnBallStopped += OnBallStopped;
            launcher.LaunchAlongPath(trajectoryPath);

            // Clear current ball reference
            currentBall = null;
        }

        /// <summary>
        /// Clamps trajectory points to stay within screen bounds
        /// </summary>
        private void ClampTrajectoryToScreen(List<Vector3> trajectoryPath)
        {
            if (mainCamera == null) return;

            float vertExtent = mainCamera.orthographicSize;
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

        /// <summary>
        /// Called when a launched ball stops
        /// </summary>
        private void OnBallStopped(Ball ball)
        {
            // Process match detection and orphan detection
            MatchingManager.Instance.ProcessBallStopped(ball);

            // Spawn next ball
            SpawnNewBall();

            // Re-enable launching
            canLaunch = true;
        }

        /// <summary>
        /// Enables/disables the launcher
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            canLaunch = enabled;

            if (!enabled && trajectoryPredictor != null)
            {
                trajectoryPredictor.HideTrajectory();
            }
        }
    }
}
