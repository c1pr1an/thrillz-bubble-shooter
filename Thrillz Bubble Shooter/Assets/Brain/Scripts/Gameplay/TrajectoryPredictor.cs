using System.Collections.Generic;
using UnityEngine;

namespace Brain.Gameplay
{
    public class TrajectoryPredictor : MonoBehaviour
    {
        // Private Fields
        [Header("Trajectory Settings")]
        [SerializeField] private int _maxBounces = 3; // Number of wall bounces to predict
        [SerializeField] private float _maxDistance = 50f; // Maximum raycast distance
        [SerializeField] private float _ballRadius = 0.35f; // Ball collision radius

        [Header("Visualization")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private float _lineWidth = 0.15f;

        private Camera _mainCamera;
        private List<Vector3> _trajectoryPoints = new List<Vector3>();

        // Properties
        public LineRenderer TrajectoryLine => _trajectoryLine;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            // Configure the assigned LineRenderer reference
            if (_trajectoryLine != null)
            {
                _trajectoryLine.startWidth = _lineWidth;
                _trajectoryLine.endWidth = _lineWidth;

                // Start with it disabled
                _trajectoryLine.enabled = false;
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
            _trajectoryPoints.Clear();
            _trajectoryPoints.Add(startPos);

            Vector2 currentPos = startPos;
            Vector2 currentDir = direction.normalized;
            float remainingDistance = _maxDistance;

            // Calculate screen bounds for wall detection (no radius offset for raycast)
            float vertExtent = _mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            Vector2 screenBoundsMin = new Vector2(-horzExtent, -vertExtent);
            Vector2 screenBoundsMax = new Vector2(horzExtent, vertExtent);

            for (int bounce = 0; bounce <= _maxBounces && remainingDistance > 0; bounce++)
            {
                // Cast ahead to find collision using raycast
                RaycastHit2D hit = Physics2D.Raycast(
                    currentPos,
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
                        _trajectoryPoints.Add(hit.point);
                        break; // Stop trajectory at ball collision
                    }
                }
                else if (hitWall && distanceToWall < remainingDistance)
                {
                    // Hit a wall first
                    _trajectoryPoints.Add(wallHitPoint);

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
                    _trajectoryPoints.Add(endPoint);
                    break;
                }
            }

            // If we only have start point, add at least an endpoint for visualization
            if (_trajectoryPoints.Count == 1)
            {
                Vector2 endPoint = (Vector2)startPos + direction.normalized * 5f;
                _trajectoryPoints.Add(endPoint);
            }

            return _trajectoryPoints;
        }

        /// <summary>
        /// Displays the calculated trajectory using LineRenderer
        /// </summary>
        public void ShowTrajectory(Vector3 startPos, Vector2 direction)
        {
            if (_trajectoryLine == null) return;

            List<Vector3> points = CalculateTrajectory(startPos, direction);

            if (points.Count < 2)
            {
                _trajectoryLine.enabled = false;
                return;
            }

            // Ensure all points have correct Z position (in front of background, behind UI)
            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Vector3(points[i].x, points[i].y, 0f);
            }

            // Update line renderer with trajectory points
            _trajectoryLine.positionCount = points.Count;
            _trajectoryLine.SetPositions(points.ToArray());
            _trajectoryLine.enabled = true;
        }

        /// <summary>
        /// Hides the trajectory line
        /// </summary>
        public void HideTrajectory()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
        }
    }
}