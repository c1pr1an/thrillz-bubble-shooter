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
        [Range(0f, 0.5f)]
        [Tooltip("Collision check radius as percentage of ball radius (0.15 = 15%)")]
        [SerializeField] private float _trajectoryCheckRadiusPercent = 0.15f; // CircleCast radius percentage

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

            // Calculate check radius for CircleCast
            float checkRadius = _ballRadius * _trajectoryCheckRadiusPercent;

            // Calculate screen bounds for wall detection (adjust for check radius)
            float vertExtent = _mainCamera.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            Vector2 screenBoundsMin = new Vector2(-horzExtent + checkRadius, -vertExtent);
            Vector2 screenBoundsMax = new Vector2(horzExtent - checkRadius, vertExtent);

            for (int bounce = 0; bounce <= _maxBounces && remainingDistance > 0; bounce++)
            {
                // Cast ahead to find collision using CircleCast for more realistic collision detection
                RaycastHit2D hit = Physics2D.CircleCast(
                    currentPos,
                    checkRadius,  // Small percentage of actual ball radius
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
                        // Ball collision - stop at hit point
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

                    // Check if there's an edge ball blocking this bounce path
                    if (IsBlockedByEdgeBall(wallHitPoint, wallNormal))
                    {
                        // Can't bounce here - edge ball blocks the gap
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

        /// <summary>
        /// Checks if a ball is on the edge column of the hex grid
        /// </summary>
        private bool IsEdgeColumnBall(Ball ball)
        {
            int column = ball.Position.x;
            int maxCols = Util.GridUtils.GetMaxColumns(ball.Position.y);
            return column == 0 || column == maxCols - 1;
        }

        /// <summary>
        /// Checks if there's an edge ball blocking the path to a wall hit point
        /// </summary>
        private bool IsBlockedByEdgeBall(Vector2 wallHitPoint, Vector2 wallNormal)
        {
            // Only check for side walls (left/right)
            if (wallNormal != Vector2.right && wallNormal != Vector2.left)
                return false;

            // Define the area near the wall hit point to check for edge balls
            float searchRadius = _ballRadius * 1.5f; // Search for balls within 1.5x ball radius

            // Find all balls near the wall hit point
            Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(wallHitPoint, searchRadius, LayerMask.GetMask("Default"));

            foreach (var collider in nearbyColliders)
            {
                Ball ball = collider.GetComponent<Ball>();
                if (ball != null && ball.HasFlag(BallFlags.Pinned))
                {
                    // Check if this is an edge ball
                    if (IsEdgeColumnBall(ball))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}