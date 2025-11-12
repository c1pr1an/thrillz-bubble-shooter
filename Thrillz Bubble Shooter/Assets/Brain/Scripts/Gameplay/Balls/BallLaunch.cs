using System;
using System.Collections.Generic;
using Brain.Audio;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    [RequireComponent(typeof(Ball))]
    public class BallLaunch : MonoBehaviour
    {
        // Private Fields
        [Header("Launch Settings")]
        [SerializeField] private float _speed = 33f;

        private Ball _ball;
        private CircleCollider2D _circleCollider;
        private bool _isMoving = false;
        private RocketBall _rocketBall; // Reference to rocket component if present

        // Trajectory path data
        private List<Vector3> _trajectoryPath;
        private int _currentSegmentIndex = 0;
        private Vector3 _segmentStart;
        private Vector3 _segmentEnd;
        private float _segmentLength;
        private float _segmentProgress;

        // Events
        public Action<Ball> OnBallStopped;

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _circleCollider = GetComponent<CircleCollider2D>();
            _rocketBall = GetComponent<RocketBall>(); // Get rocket component if present
        }

        public void LaunchAlongPath(List<Vector3> path)
        {
            if (path == null || path.Count < 2)
            {
                Debug.LogError("BallLaunch: Invalid trajectory path!");
                return;
            }

            _trajectoryPath = new List<Vector3>(path);
            _currentSegmentIndex = 0;
            InitializeSegment(0);
            _isMoving = true;

            // Disable collider during launch - ball ignores all collisions
            _circleCollider.enabled = false;
            if (_ball.IsRocket())
                SoundManager.Instance.PlaySfxOneShot(SoundType.Game_Rocket_Launch);
            else if (_ball.IsLightning())
                SoundManager.Instance.PlaySfxOneShot(SoundType.Game_Electricity_Launch);
            else SoundManager.Instance.PlaySfxOneShot(SoundType.Game_BallShoot);
        }

        private void InitializeSegment(int segmentIndex)
        {
            if (segmentIndex >= _trajectoryPath.Count - 1)
            {
                // Reached end of path
                StopBall();
                return;
            }

            _segmentStart = _trajectoryPath[segmentIndex];
            _segmentEnd = _trajectoryPath[segmentIndex + 1];
            _segmentLength = Vector3.Distance(_segmentStart, _segmentEnd);
            _segmentProgress = 0f;

            // Position at start of segment
            transform.position = _segmentStart;

            // Update rocket rotation to match new segment direction
            if (_rocketBall != null)
            {
                Vector2 segmentDirection = (_segmentEnd - _segmentStart).normalized;
                // Pass true for isBounce when not the first segment (segment 0)
                bool isBounce = segmentIndex > 0;
                _rocketBall.UpdateFlightRotation(segmentDirection, isBounce);
            }
        }

        private void Update()
        {
            if (!_isMoving || _trajectoryPath == null) return;

            // Move along current segment
            _segmentProgress += Time.deltaTime * _speed;

            if (_segmentProgress >= _segmentLength)
            {
                // Completed current segment, move to next
                _currentSegmentIndex++;

                if (_currentSegmentIndex >= _trajectoryPath.Count - 1)
                {
                    // Reached end of path
                    transform.position = _trajectoryPath[^1];
                    StopBall();
                }
                else
                {
                    // Move to next segment
                    InitializeSegment(_currentSegmentIndex);
                    SoundManager.Instance.PlaySfxOneShot(SoundType.Game_BallSideBounce);
                }
            }
            else
            {
                // Interpolate along current segment
                float t = _segmentProgress / _segmentLength;
                transform.position = Vector3.Lerp(_segmentStart, _segmentEnd, t);
            }
        }

        private void StopBall()
        {
            _isMoving = false;

            // Handle rocket ball stopping
            if (_rocketBall != null && _trajectoryPath != null && _trajectoryPath.Count >= 2)
            {
                // Get the direction of the last segment (impact direction)
                Vector3 lastSegmentStart = _trajectoryPath[Mathf.Max(0, _trajectoryPath.Count - 2)];
                Vector3 lastSegmentEnd = _trajectoryPath[_trajectoryPath.Count - 1];
                Vector2 impactDirection = (lastSegmentEnd - lastSegmentStart).normalized;

                // Store the direction for forward movement
                _rocketBall.SetLastVelocity(impactDirection);
            }

            // Re-enable collider now that ball is at final position
            _circleCollider.enabled = true;

            // Add ball to grid - rocket balls get special handling
            GridManager.Instance.AddBallToGrid(_ball, transform.position);

            // Trigger stopped event
            OnBallStopped?.Invoke(_ball);

            // Animate rocket forward movement after detection
            if (_rocketBall != null)
            {
                _rocketBall.AnimateForwardMovement();
            }

            // Destroy this launch component (no longer needed)
            Destroy(this);
        }

        private void OnDrawGizmos()
        {
            if (!_isMoving || _trajectoryPath == null) return;

            // Visualize the trajectory path
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _trajectoryPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_trajectoryPath[i], _trajectoryPath[i + 1]);
            }

            // Visualize the endpoint
            if (_trajectoryPath.Count > 0)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_trajectoryPath[^1], 0.2f);
            }

            // Show current position
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.15f);
        }
    }
}
