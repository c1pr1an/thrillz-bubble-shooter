using System;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using DG.Tweening;
using UnityEngine;

namespace Brain.Managers
{
    public class GridScrollManager : UnitySingleton<GridScrollManager>
    {
        // Serialized Fields
        [Header("Scroll Settings")]
        [SerializeField] private int _deathLineRow = 0;
        [SerializeField] private int _targetBufferRows = 4;

        [Header("Animation Settings")]
        [SerializeField] private float _scrollSpeed = 7f; // Units per second
        [SerializeField] private float _minScrollDuration = 0.15f; // Minimum duration for very small moves
        [SerializeField] private Ease _scrollEase = Ease.OutCubic;

        // Private Fields
        private Vector3 _initialGridPosition;
        private int _lastLowestRow = -1;
        private Tween _gridTween;
        private bool _isMoving = false;

        // Properties
        public bool IsMoving => _isMoving;

        // Events
        public event Action OnDeathLineTouched;

        // Unity Lifecycle
        private void Start()
        {
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                _initialGridPosition = GridManager.Instance.GridContainer.position;
            }
        }

        private void OnDestroy()
        {
            _gridTween?.Kill();
        }

        public void PreCalculateAndMoveGrid(List<Ball> ballsToDestroy)
        {
            if (ballsToDestroy == null || ballsToDestroy.Count == 0) return;

            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            // Find what the lowest row will be after these balls are destroyed
            int futureLowestRow = GetLowestOccupiedRowAfterDestruction(ballsToDestroy);
            if (futureLowestRow == -1) return;

            // Check death line collision
            if (futureLowestRow <= _deathLineRow)
            {
                OnDeathLineTouched?.Invoke();
                return;
            }

            // If we haven't tracked lowest row yet, just set it
            if (_lastLowestRow == -1)
            {
                _lastLowestRow = futureLowestRow;
                return;
            }

            // Move grid down if bottom row will be cleared
            if (futureLowestRow > _lastLowestRow)
            {
                int rowsToMove = futureLowestRow - _lastLowestRow;
                MoveGridDown(rowsToMove);
                _lastLowestRow = futureLowestRow;
            }
        }

        // Calculate what the lowest occupied row will be after destroying specific balls
        private int GetLowestOccupiedRowAfterDestruction(List<Ball> ballsToDestroy)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return -1;

            // Create a HashSet for quick lookup
            HashSet<Ball> toDestroy = new HashSet<Ball>(ballsToDestroy);

            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    // Check if ball exists and won't be destroyed
                    if (ball != null && !toDestroy.Contains(ball))
                    {
                        return row;
                    }
                }
            }

            return -1;
        }

        private void MoveGridDown(int rows)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.GridContainer == null) return;

            float moveDistance = rows * gridManager.BallHeight;
            Vector3 currentPosition = gridManager.GridContainer.position;
            Vector3 newPosition = currentPosition + new Vector3(0, -moveDistance, 0);

            // Don't move above initial position (anchor limit)
            if (newPosition.y > _initialGridPosition.y)
            {
                newPosition.y = _initialGridPosition.y;
            }

            // Kill existing tween to prevent conflicts
            _gridTween?.Kill();

            // Set moving flag
            _isMoving = true;

            // Tween the grid container with fixed 0.2 second duration
            _gridTween = gridManager.GridContainer
                .DOMove(newPosition, 0.2f)
                .SetEase(_scrollEase)
                .OnComplete(() => _isMoving = false);
        }

        public void ResetScroll()
        {
            _lastLowestRow = -1;

            // Kill existing tween
            _gridTween?.Kill();

            // Reset grid position with tween (phantom balls move automatically as children)
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                // Calculate duration based on distance and speed
                Vector3 currentPosition = GridManager.Instance.GridContainer.position;
                float distance = Vector3.Distance(currentPosition, _initialGridPosition);
                float duration = Mathf.Max(distance / _scrollSpeed, _minScrollDuration);

                _gridTween = GridManager.Instance.GridContainer
                    .DOMove(_initialGridPosition, duration)
                    .SetEase(_scrollEase);
            }
        }

        // Public method to instantly reset without animation (useful for level start)
        public void ResetScrollInstant()
        {
            _lastLowestRow = -1;

            // Kill existing tween
            _gridTween?.Kill();

            // Reset position instantly
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                GridManager.Instance.GridContainer.position = _initialGridPosition;
            }
        }
    }
}
