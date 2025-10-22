using System;
using System.Collections.Generic;
using Brain.Managers;
using UnityEngine;

namespace Brain.Gameplay
{
    [RequireComponent(typeof(Ball))]
    public class BallLaunch : MonoBehaviour
    {
        // Private Fields
        [Header("Launch Settings")]
        [SerializeField] private float _speed = 25f;

        private Ball _ball;
        private CircleCollider2D _circleCollider;
        private bool _isMoving = false;

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

            // Re-enable collider now that ball is at final position
            _circleCollider.enabled = true;

            // Add ball to grid at the position it's already at (pre-snapped)
            // The ball is already at the grid snap position, so AddBallToGrid
            // will find the same position and just register it in the grid
            GridManager.Instance.AddBallToGrid(_ball, transform.position);

            // Trigger stopped event
            OnBallStopped?.Invoke(_ball);

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
