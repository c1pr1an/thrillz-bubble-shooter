using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;

namespace Brain.Gameplay
{
    public class TrajectoryPredictor : UnitySingleton<TrajectoryPredictor>
    {
        [Header("Trajectory Settings")]
        [SerializeField] private int _maxBounces = 3;
        [SerializeField] private float _maxDistance = 50f;
        [SerializeField] private float _ballRadius = 0.35f;
        [Range(0f, 0.5f)]
        [Tooltip("Collision check radius as percentage of ball radius (0.15 = 15%)")]
        [SerializeField] private float _trajectoryCheckRadiusPercent = 0.15f;

        [Header("Visualization")]
        [SerializeField] private LineRenderer _trajectoryLine;
        [SerializeField] private float _lineWidth = 0.15f;
        [SerializeField] private float _lineAlpha = 0.8f;

        private Camera _mainCamera;
        private List<Vector3> _trajectoryPoints = new List<Vector3>();
        private Vector2 _predictedImpactPosition;
        private bool _hasValidPrediction = false;

        public LineRenderer TrajectoryLine => _trajectoryLine;
        public Vector2 PredictedImpactPosition => _predictedImpactPosition;
        public bool HasValidPrediction => _hasValidPrediction;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        private void Start()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.startWidth = _lineWidth;
                _trajectoryLine.endWidth = _lineWidth;
                _trajectoryLine.enabled = false;
            }
            else
            {
                Debug.LogError("TrajectoryPredictor: No LineRenderer assigned! Please assign one in the Inspector.");
            }
        }

        public List<Vector3> CalculateTrajectory(Vector3 startPos, Vector2 direction)
        {
            _trajectoryPoints.Clear();
            _trajectoryPoints.Add(startPos);

            Vector2 currentPos = startPos;
            Vector2 currentDir = direction.normalized;
            float remainingDistance = _maxDistance;

            float checkRadius = _ballRadius * _trajectoryCheckRadiusPercent;

            float vertExtent = _mainCamera.orthographicSize;
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

            for (int i = 0; i < points.Count; i++)
            {
                points[i] = new Vector3(points[i].x, points[i].y, 0f);
            }

            // Set line color to match ball color from prefab
            Color lineColor = ball.DisplayColor;
            lineColor.a = _lineAlpha;
            _trajectoryLine.startColor = lineColor;
            _trajectoryLine.endColor = lineColor;

            _trajectoryLine.positionCount = points.Count;
            _trajectoryLine.SetPositions(points.ToArray());
            _trajectoryLine.enabled = true;
        }

        public void HideTrajectory()
        {
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
            _hasValidPrediction = false;
        }
    }
}