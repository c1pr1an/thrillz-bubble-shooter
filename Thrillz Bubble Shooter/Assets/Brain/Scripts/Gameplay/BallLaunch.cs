using System;
using System.Collections.Generic;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Handles ball movement along predetermined trajectory path
    /// Ball follows the exact trajectory including wall bounces, ignoring ball collisions
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BallLaunch : MonoBehaviour
    {
        [Header("Launch Settings")]
        [SerializeField] private float speed = 15f;

        private Ball ball;
        private CircleCollider2D circleCollider;
        private bool isMoving = false;

        // Trajectory path data
        private List<Vector3> trajectoryPath;
        private int currentSegmentIndex = 0;
        private Vector3 segmentStart;
        private Vector3 segmentEnd;
        private float segmentLength;
        private float segmentProgress;

        // Event when ball stops
        public Action<Ball> OnBallStopped;

        private void Awake()
        {
            ball = GetComponent<Ball>();
            circleCollider = GetComponent<CircleCollider2D>();
        }

        /// <summary>
        /// Launches the ball along a specific trajectory path with wall bounces
        /// </summary>
        public void LaunchAlongPath(List<Vector3> path)
        {
            if (path == null || path.Count < 2)
            {
                Debug.LogError("BallLaunch: Invalid trajectory path!");
                return;
            }

            trajectoryPath = new List<Vector3>(path);
            currentSegmentIndex = 0;
            InitializeSegment(0);
            isMoving = true;

            // Disable collider during launch - ball ignores all collisions
            circleCollider.enabled = false;
        }

        /// <summary>
        /// Initialize movement for a segment of the trajectory
        /// </summary>
        private void InitializeSegment(int segmentIndex)
        {
            if (segmentIndex >= trajectoryPath.Count - 1)
            {
                // Reached end of path
                StopBall();
                return;
            }

            segmentStart = trajectoryPath[segmentIndex];
            segmentEnd = trajectoryPath[segmentIndex + 1];
            segmentLength = Vector3.Distance(segmentStart, segmentEnd);
            segmentProgress = 0f;

            // Position at start of segment
            transform.position = segmentStart;
        }

        private void Update()
        {
            if (!isMoving || trajectoryPath == null) return;

            // Move along current segment
            segmentProgress += Time.deltaTime * speed;

            if (segmentProgress >= segmentLength)
            {
                // Completed current segment, move to next
                currentSegmentIndex++;

                if (currentSegmentIndex >= trajectoryPath.Count - 1)
                {
                    // Reached end of path
                    transform.position = trajectoryPath[trajectoryPath.Count - 1];
                    StopBall();
                }
                else
                {
                    // Move to next segment
                    InitializeSegment(currentSegmentIndex);
                }
            }
            else
            {
                // Interpolate along current segment
                float t = segmentProgress / segmentLength;
                transform.position = Vector3.Lerp(segmentStart, segmentEnd, t);
            }
        }

        /// <summary>
        /// Stops the ball and adds it to the grid
        /// Since the endpoint is already the snap position, no additional snapping needed
        /// </summary>
        private void StopBall()
        {
            isMoving = false;

            // Re-enable collider now that ball is at final position
            circleCollider.enabled = true;

            // Add ball to grid at the position it's already at (pre-snapped)
            // The ball is already at the grid snap position, so AddBallToGrid
            // will find the same position and just register it in the grid
            GridManager.Instance.AddBallToGrid(ball, transform.position);

            // Trigger stopped event
            OnBallStopped?.Invoke(ball);

            // Destroy this launch component (no longer needed)
            Destroy(this);
        }

        private void OnDrawGizmos()
        {
            if (!isMoving || trajectoryPath == null) return;

            // Visualize the trajectory path
            Gizmos.color = Color.yellow;
            for (int i = 0; i < trajectoryPath.Count - 1; i++)
            {
                Gizmos.DrawLine(trajectoryPath[i], trajectoryPath[i + 1]);
            }

            // Visualize the endpoint
            if (trajectoryPath.Count > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(trajectoryPath[trajectoryPath.Count - 1], 0.2f);
            }

            // Show current position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}
