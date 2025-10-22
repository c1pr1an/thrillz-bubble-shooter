using System;
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
        [SerializeField] private float _scrollSpeed = 5f; // Units per second
        [SerializeField] private float _minScrollDuration = 0.2f; // Minimum duration for very small moves
        [SerializeField] private Ease _scrollEase = Ease.OutCubic;

        // Private Fields
        private Vector3 _initialGridPosition;
        private int _lastLowestRow = -1;
        private Tween _gridTween;

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

        // Public Methods
        public void UpdateGridPosition()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            int lowestRow = GetLowestOccupiedRow();
            if (lowestRow == -1) return;

            // Check death line collision
            if (lowestRow <= _deathLineRow)
            {
                OnDeathLineTouched?.Invoke();
                return;
            }

            // Move grid up if bottom row cleared
            if (lowestRow > _lastLowestRow && _lastLowestRow != -1)
            {
                int rowsToMove = lowestRow - _lastLowestRow;
                MoveGridUp(rowsToMove);
            }

            _lastLowestRow = lowestRow;
        }

        // Private Methods
        private int GetLowestOccupiedRow()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return -1;

            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    if (gridManager.Balls[row][col] != null)
                    {
                        return row;
                    }
                }
            }

            return -1;
        }

        private void MoveGridUp(int rows)
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

            // Calculate duration based on distance and speed
            float actualDistance = Vector3.Distance(currentPosition, newPosition);
            float duration = Mathf.Max(actualDistance / _scrollSpeed, _minScrollDuration);

            // Kill existing tween to prevent conflicts
            _gridTween?.Kill();

            // Tween the grid container (phantom balls move automatically as children)
            _gridTween = gridManager.GridContainer
                .DOMove(newPosition, duration)
                .SetEase(_scrollEase);
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
