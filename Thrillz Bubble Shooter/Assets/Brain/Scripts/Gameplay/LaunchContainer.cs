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
        [SerializeField] private LineRenderer aimLine;
        [SerializeField] private float aimLineLength = 3f;
        [SerializeField] private float minAimAngle = 10f; // Min angle from horizontal (in degrees)
        [SerializeField] private float maxAimAngle = 170f; // Max angle from horizontal (in degrees)

        private Ball currentBall;
        private Camera mainCamera;
        private bool canLaunch = true;

        private void Awake()
        {
            mainCamera = Camera.main;

            // Setup aim line
            if (aimLine != null)
            {
                aimLine.positionCount = 2;
                aimLine.enabled = false;
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

            // Update aim line
            if (aimLine != null)
            {
                aimLine.enabled = true;
                aimLine.SetPosition(0, ballSpawnPoint.position);
                aimLine.SetPosition(1, ballSpawnPoint.position + (Vector3)aimDirection * aimLineLength);
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

            if (!GridManager.Exists())
            {
                Debug.LogError("LaunchContainer: GridManager not found!");
                return;
            }

            // Pick a random color
            BallColor randomColor = (BallColor)Random.Range(0, 6);
            int colorIndex = (int)randomColor;

            // Get prefab from GridManager (single source of truth)
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

            // Disable launching until ball stops
            canLaunch = false;

            // Hide aim line
            if (aimLine != null)
            {
                aimLine.enabled = false;
            }

            // Unparent ball from spawn point
            currentBall.transform.SetParent(null);

            // Add launch component
            BallLaunch launcher = currentBall.gameObject.AddComponent<BallLaunch>();
            launcher.OnBallStopped += OnBallStopped;
            launcher.Launch(direction);

            // Clear current ball reference
            currentBall = null;
        }

        /// <summary>
        /// Called when a launched ball stops
        /// </summary>
        private void OnBallStopped(Ball ball)
        {
            // Process match detection and orphan detection
            if (MatchingManager.Exists())
            {
                MatchingManager.Instance.ProcessBallStopped(ball);
            }

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

            if (aimLine != null)
            {
                aimLine.enabled = enabled;
            }
        }
    }
}
