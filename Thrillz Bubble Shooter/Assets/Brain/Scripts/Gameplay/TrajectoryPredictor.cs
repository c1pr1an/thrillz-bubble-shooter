using System.Collections.Generic;
using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Calculates and visualizes ball trajectory including wall bounces
    /// Inspired by BubbleShooterGameToolkit's trajectory system
    /// </summary>
    public class TrajectoryPredictor : MonoBehaviour
    {
        [Header("Trajectory Settings")]
        [SerializeField] private int maxBounces = 3; // Number of wall bounces to predict
        [SerializeField] private float maxDistance = 50f; // Maximum raycast distance
        [SerializeField] private float ballRadius = 0.35f; // Ball collision radius

        [Header("Visualization")]
        [SerializeField] private LineRenderer trajectoryLine;
        [SerializeField] private float lineWidth = 0.15f;

        private Camera mainCamera;
        private List<Vector3> trajectoryPoints = new List<Vector3>();

        // Public getter for checking conflicts
        public LineRenderer TrajectoryLine => trajectoryLine;

        private void Awake()
        {
            mainCamera = Camera.main;
        }

        private void Start()
        {
            // Configure the assigned LineRenderer reference
            if (trajectoryLine != null)
            {
                trajectoryLine.startWidth = lineWidth;
                trajectoryLine.endWidth = lineWidth;

                // Start with it disabled
                trajectoryLine.enabled = false;
            }
            else
            {
                Debug.LogError("TrajectoryPredictor: No LineRenderer assigned! Please assign one in the Inspector.");
            }
        }

        /// <summary>
        /// Calculates trajectory with wall bounces
        /// </summary>
        public List<Vector3> CalculateTrajectory(Vector3 startPos, Vector2 direction)
        {
            trajectoryPoints.Clear();
            trajectoryPoints.Add(startPos);

            Vector2 currentPos = startPos;
            Vector2 currentDir = direction.normalized;
            float remainingDistance = maxDistance;

            // Calculate screen bounds for wall detection
            float vertExtent = mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            Vector2 screenBoundsMin = new Vector2(-horzExtent + ballRadius, -vertExtent);
            Vector2 screenBoundsMax = new Vector2(horzExtent - ballRadius, vertExtent);

            for (int bounce = 0; bounce <= maxBounces && remainingDistance > 0; bounce++)
            {
                // Cast ahead to find collision
                RaycastHit2D hit = Physics2D.CircleCast(
                    currentPos,
                    ballRadius,
                    currentDir,
                    remainingDistance,
                    LayerMask.GetMask("Default")
                );

                // Check wall collision first (walls are closer than balls usually)
                float distanceToWall = float.MaxValue;
                Vector2 wallHitPoint = Vector2.zero;
                Vector2 wallNormal = Vector2.zero;
                bool hitWall = false;

                // Check left wall
                if (currentDir.x < 0)
                {
                    float t = (screenBoundsMin.x - currentPos.x) / currentDir.x;
                    if (t > 0 && t < distanceToWall)
                    {
                        distanceToWall = t;
                        wallHitPoint = currentPos + currentDir * t;
                        wallNormal = Vector2.right;
                        hitWall = true;
                    }
                }
                // Check right wall
                else if (currentDir.x > 0)
                {
                    float t = (screenBoundsMax.x - currentPos.x) / currentDir.x;
                    if (t > 0 && t < distanceToWall)
                    {
                        distanceToWall = t;
                        wallHitPoint = currentPos + currentDir * t;
                        wallNormal = Vector2.left;
                        hitWall = true;
                    }
                }

                // Check top boundary
                if (currentDir.y > 0)
                {
                    float t = (screenBoundsMax.y - currentPos.y) / currentDir.y;
                    if (t > 0 && t < distanceToWall)
                    {
                        distanceToWall = t;
                        wallHitPoint = currentPos + currentDir * t;
                        wallNormal = Vector2.down;
                        hitWall = true;
                    }
                }

                // Determine what we hit first - wall or ball
                bool hitBall = hit.collider != null;
                float distanceToBall = hitBall ? hit.distance : float.MaxValue;

                if (hitBall && distanceToBall < distanceToWall)
                {
                    // Hit a ball first
                    Ball hitBallComponent = hit.collider.GetComponent<Ball>();
                    if (hitBallComponent != null && hitBallComponent.HasFlag(BallFlags.Pinned))
                    {
                        // Add point where we hit the ball
                        trajectoryPoints.Add(hit.point);
                        break; // Stop trajectory at ball collision
                    }
                }
                else if (hitWall && distanceToWall < remainingDistance)
                {
                    // Hit a wall first
                    trajectoryPoints.Add(wallHitPoint);

                    // If we hit the top, stop here
                    if (wallNormal == Vector2.down)
                    {
                        break;
                    }

                    // Reflect direction for bounce
                    currentDir = Vector2.Reflect(currentDir, wallNormal);
                    currentPos = wallHitPoint;
                    remainingDistance -= distanceToWall;
                }
                else
                {
                    // No collision within remaining distance - add endpoint
                    Vector2 endPoint = currentPos + currentDir * Mathf.Min(remainingDistance, 10f);
                    trajectoryPoints.Add(endPoint);
                    break;
                }
            }

            // If we only have start point, add at least an endpoint for visualization
            if (trajectoryPoints.Count == 1)
            {
                Vector2 endPoint = (Vector2)startPos + direction.normalized * 5f;
                trajectoryPoints.Add(endPoint);
            }

            return trajectoryPoints;
        }

        /// <summary>
        /// Displays the calculated trajectory using LineRenderer
        /// </summary>
        public void ShowTrajectory(Vector3 startPos, Vector2 direction)
        {
            if (trajectoryLine == null) return;

            List<Vector3> points = CalculateTrajectory(startPos, direction);

            if (points.Count < 2)
            {
                trajectoryLine.enabled = false;
                return;
            }

            // Ensure all points have correct Z position (in front of background, behind UI)
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Vector3(points[i].x, points[i].y, 0f);
            }

            // Update line renderer with trajectory points
            trajectoryLine.positionCount = points.Count;
            trajectoryLine.SetPositions(points.ToArray());
            trajectoryLine.enabled = true;
        }

        /// <summary>
        /// Hides the trajectory line
        /// </summary>
        public void HideTrajectory()
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
        }
    }
}