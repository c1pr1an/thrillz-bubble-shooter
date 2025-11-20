using System;
using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;

namespace Brain.Gameplay
{
    public class TrajectoryPredictor : UnitySingleton<TrajectoryPredictor>
    {
        // Events for trajectory updates
        public event Action<Vector2, Vector2, Ball> OnTrajectoryUpdated; // impactPos, direction, ball
        public event Action OnTrajectoryHidden;

        [Header("Trajectory Settings")]
        [SerializeField] private int _maxBounces = 3;
        [SerializeField] private float _maxDistance = 50f;
        [SerializeField] private float _ballRadius = 0.35f;
        [Range(0f, 0.5f)]
        [Tooltip("Collision check radius as percentage of ball radius (0.15 = 15%)")]
        [SerializeField] private float _trajectoryCheckRadiusPercent = 0.15f;

        [Header("Visualization")]
        [SerializeField] private LineRenderer _trajectoryLine;

        private List<Vector3> _trajectoryPoints = new List<Vector3>();
        private Vector2 _predictedImpactPosition;
        private bool _hasValidPrediction = false;

        public LineRenderer TrajectoryLine => _trajectoryLine;
        public Vector2 PredictedImpactPosition => _predictedImpactPosition;
        public bool HasValidPrediction => _hasValidPrediction;
        public List<Vector3> CurrentTrajectoryPoints => _trajectoryPoints;

        private void Start()
        {
            _trajectoryLine.enabled = false;
        }

        public List<Vector3> CalculateTrajectory(Vector3 startPos, Vector2 direction)
        {
            _trajectoryPoints.Clear();
            _trajectoryPoints.Add(startPos);

            Vector2 currentPos = startPos;
            Vector2 currentDir = direction.normalized;
            float remainingDistance = _maxDistance;

            float checkRadius = _ballRadius * _trajectoryCheckRadiusPercent;

            float vertExtent = Cameras.Instance.MainCam.orthographicSize;
            float horzExtent = vertExtent * Screen.width / Screen.height;
            Vector2 screenBoundsMin = new Vector2(-horzExtent + checkRadius, -vertExtent);
            Vector2 screenBoundsMax = new Vector2(horzExtent - checkRadius, vertExtent);

            for (int bounce = 0; bounce <= _maxBounces && remainingDistance > 0; bounce++)
            {
                RaycastHit2D hit = Physics2D.CircleCast(
                    currentPos,
                    checkRadius,
                    currentDir,
                    remainingDistance,
                    LayerMask.GetMask("Default")
                );

                float distanceToWall = float.MaxValue;
                Vector2 wallHitPoint = Vector2.zero;
                Vector2 wallNormal = Vector2.zero;
                bool hitWall = false;

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

                bool hitBall = hit.collider != null;
                float distanceToBall = hitBall ? hit.distance : float.MaxValue;

                if (hitBall && distanceToBall < distanceToWall)
                {
                    bool isPhantom = PhantomBallManager.IsPhantomCollider(hit.collider);

                    if (isPhantom)
                    {
                        _trajectoryPoints.Add(hit.point);
                        break;
                    }
                    else
                    {
                        Ball hitBallComponent = hit.collider.GetComponent<Ball>();
                        if (hitBallComponent != null && hitBallComponent.HasFlag(BallFlags.Pinned))
                        {
                            _trajectoryPoints.Add(hit.point);
                            break;
                        }
                    }
                }
                else if (hitWall && distanceToWall < remainingDistance)
                {
                    _trajectoryPoints.Add(wallHitPoint);

                    if (wallNormal == Vector2.down)
                    {
                        break;
                    }

                    currentDir = Vector2.Reflect(currentDir, wallNormal);
                    currentPos = wallHitPoint;
                    remainingDistance -= distanceToWall;
                }
                else
                {
                    Vector2 endPoint = currentPos + currentDir * Mathf.Min(remainingDistance, 10f);
                    _trajectoryPoints.Add(endPoint);
                    break;
                }
            }

            if (_trajectoryPoints.Count == 1)
            {
                Vector2 endPoint = (Vector2)startPos + direction.normalized * 5f;
                _trajectoryPoints.Add(endPoint);
            }

            return _trajectoryPoints;
        }

        public void ShowTrajectory(Vector3 startPos, Vector2 direction, Ball ball)
        {
            if (_trajectoryLine == null) return;

            List<Vector3> points = CalculateTrajectory(startPos, direction);

            if (points.Count < 2)
            {
                _trajectoryLine.enabled = false;
                _hasValidPrediction = false;
                return;
            }

            // Set the predicted impact position (last point in trajectory)
            _predictedImpactPosition = points[points.Count - 1];
            _hasValidPrediction = true;

            // Fire event for trajectory update (for bonus ball preview)
            Vector2 lastDirection = GetLastSegmentDirection();
            OnTrajectoryUpdated?.Invoke(_predictedImpactPosition, lastDirection, ball);

            // Create smoothed points list with extra points at corners
            List<Vector3> smoothedPoints = new List<Vector3>();

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 currentPoint = new Vector3(points[i].x, points[i].y, 0f);

                // For corner points (not first or last), add extra points for smoothing
                if (i > 0 && i < points.Count - 1)
                {
                    Vector3 prevPoint = new Vector3(points[i - 1].x, points[i - 1].y, 0f);
                    Vector3 nextPoint = new Vector3(points[i + 1].x, points[i + 1].y, 0f);

                    // Calculate approach and departure vectors
                    Vector3 approachDir = (currentPoint - prevPoint).normalized;
                    Vector3 departDir = (nextPoint - currentPoint).normalized;

                    float cornerOffset = _ballRadius * 0.1f;
                    Vector3 beforeCorner = currentPoint - approachDir * cornerOffset;
                    smoothedPoints.Add(beforeCorner);
                    smoothedPoints.Add(beforeCorner);

                    // Add the actual corner point
                    smoothedPoints.Add(currentPoint);
                    smoothedPoints.Add(currentPoint);

                    // Add a point slightly after the corner
                    Vector3 afterCorner = currentPoint + departDir * cornerOffset;
                    smoothedPoints.Add(afterCorner);
                    smoothedPoints.Add(afterCorner);
                }
                else
                {
                    // For first and last points, just add them directly
                    smoothedPoints.Add(currentPoint);
                }
            }

            // Set line color to match ball color from prefab
            Color lineColor = ball.DisplayColor;
            _trajectoryLine.startColor = lineColor;
            _trajectoryLine.endColor = lineColor;

            _trajectoryLine.positionCount = smoothedPoints.Count;
            _trajectoryLine.SetPositions(smoothedPoints.ToArray());
            _trajectoryLine.enabled = true;
        }

        /// <summary>
        /// Get the direction of the last segment in the trajectory (for accurate impact angle)
        /// </summary>
        public Vector2 GetLastSegmentDirection()
        {
            if (_trajectoryPoints.Count < 2)
                return Vector2.up; // Default direction if no valid trajectory

            // Get the last two points to determine the final impact direction
            Vector3 secondToLast = _trajectoryPoints[_trajectoryPoints.Count - 2];
            Vector3 last = _trajectoryPoints[_trajectoryPoints.Count - 1];

            return ((Vector2)(last - secondToLast)).normalized;
        }

        public void HideTrajectory()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
            _hasValidPrediction = false;

            // Fire event for trajectory hidden (to hide bonus ball preview)
            OnTrajectoryHidden?.Invoke();
        }
    }
}