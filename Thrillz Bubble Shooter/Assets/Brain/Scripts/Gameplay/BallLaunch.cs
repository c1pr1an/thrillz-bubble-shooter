using System;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Handles ball movement with custom trajectory physics
    /// Uses manual movement and collision detection (no Rigidbody physics)
    /// Adapted from BubbleShooterGameToolkit reference implementation
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallLaunch : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float speed = 15f;
        [SerializeField] private float checkDistance = 0.5f; // How far ahead to check for collisions

        private Ball ball;
        private CircleCollider2D circleCollider;
        private bool isLaunched = false;
        private Vector2 direction;
        private Camera mainCamera;
        private Vector2 screenBoundsMin;
        private Vector2 screenBoundsMax;

        // Event when ball stops
        public Action<Ball> OnBallStopped;

        private void Awake()
        {
            ball = GetComponent<Ball>();
            circleCollider = GetComponent<CircleCollider2D>();
            mainCamera = Camera.main;
        }

        /// <summary>
        /// Launches the ball in the given direction
        /// </summary>
        public void Launch(Vector2 launchDirection)
        {
            direction = launchDirection.normalized;
            isLaunched = true;

            // Calculate screen bounds for wall bouncing
            CalculateScreenBounds();

            // Disable collider temporarily to avoid self-collision with launch container
            circleCollider.enabled = false;
            Invoke(nameof(EnableCollider), 0.1f);
        }

        private void EnableCollider()
        {
            if (circleCollider != null)
            {
                circleCollider.enabled = true;
            }
        }

        private void FixedUpdate()
        {
            if (!isLaunched) return;

            // Move ball
            Vector3 movement = direction * speed * Time.fixedDeltaTime;
            transform.position += movement;

            // Check for wall bouncing
            CheckWallBounce();

            // Check for collision with grid balls
            CheckBallCollision();
        }

        /// <summary>
        /// Calculates screen bounds for wall bouncing
        /// </summary>
        private void CalculateScreenBounds()
        {
            if (mainCamera == null) return;

            float vertExtent = mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;

            screenBoundsMin = new Vector2(-horzExtent, -vertExtent);
            screenBoundsMax = new Vector2(horzExtent, vertExtent);
        }

        /// <summary>
        /// Checks if ball hits screen edges and bounces
        /// </summary>
        private void CheckWallBounce()
        {
            float ballRadius = circleCollider.radius;

            // Check left/right bounds
            if (transform.position.x - ballRadius < screenBoundsMin.x)
            {
                // Hit left wall - reflect horizontal direction
                transform.position = new Vector3(screenBoundsMin.x + ballRadius, transform.position.y, transform.position.z);
                direction = Vector2.Reflect(direction, Vector2.right);
            }
            else if (transform.position.x + ballRadius > screenBoundsMax.x)
            {
                // Hit right wall - reflect horizontal direction
                transform.position = new Vector3(screenBoundsMax.x - ballRadius, transform.position.y, transform.position.z);
                direction = Vector2.Reflect(direction, Vector2.left);
            }
        }

        /// <summary>
        /// Checks for collision with grid balls using CircleCast
        /// </summary>
        private void CheckBallCollision()
        {
            float ballRadius = circleCollider.radius;

            // Cast a circle ahead to detect collisions
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position,
                ballRadius,
                direction,
                checkDistance,
                LayerMask.GetMask("Default")
            );

            if (hit.collider != null)
            {
                Ball hitBall = hit.collider.GetComponent<Ball>();

                if (hitBall != null && hitBall.HasFlag(BallFlags.Pinned))
                {
                    // Stop the ball and add to grid
                    StopBall();
                }
            }

            // Also check if ball reached the top
            if (transform.position.y >= screenBoundsMax.y - ballRadius)
            {
                StopBall();
            }
        }

        /// <summary>
        /// Stops the ball and adds it to the grid
        /// </summary>
        private void StopBall()
        {
            isLaunched = false;

            // Add ball to grid at current position
            GridManager.Instance.AddBallToGrid(ball, transform.position);

            // Trigger stopped event
            OnBallStopped?.Invoke(ball);

            // Destroy this launch component (no longer needed)
            Destroy(this);
        }

        private void OnDrawGizmos()
        {
            if (!isLaunched || circleCollider == null) return;

            // Visualize the collision check
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + (Vector3)(direction * checkDistance), circleCollider.radius);
        }
    }
}
