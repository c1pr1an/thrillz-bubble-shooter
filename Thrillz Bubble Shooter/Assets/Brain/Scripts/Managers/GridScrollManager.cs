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
        [SerializeField] private int _targetBufferRows = 4;
        [SerializeField] private float _topBoundaryY = 9.5f;

        [Header("Animation Settings")]
        [SerializeField] private float _scrollSpeed = 7f; // Units per second
        [SerializeField] private float _minScrollDuration = 0.15f; // Minimum duration for very small moves
        [SerializeField] private Ease _scrollEase = Ease.OutCubic;

        // Private Fields
        private Vector3 _initialGridPosition;
        private int _lastLowestRow = -1;
        private Tween _gridTween;
        private bool _isMoving = false;
        private bool _scrollingEnabled = false;

        // Properties
        public bool IsMoving => _isMoving;
        public bool ScrollingEnabled => _scrollingEnabled;

        public void Init()
        {
            _scrollingEnabled = true;
            _initialGridPosition = GridManager.Instance.GridContainer.position;
            _lastLowestRow = GetCurrentLowestOccupiedRow();
        }

        // Unity Lifecycle
        private void OnDestroy()
        {
            _gridTween?.Kill();
        }

        public void PreCalculateAndMoveGrid(List<Ball> ballsToDestroy)
        {
            if (ballsToDestroy == null || ballsToDestroy.Count == 0) return;

            if (!_scrollingEnabled) return;

            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            int futureLowestRow = GetLowestOccupiedRowAfterDestruction(ballsToDestroy);
            if (futureLowestRow == -1) return;

            if (_lastLowestRow == -1)
            {
                _lastLowestRow = futureLowestRow;
                return;
            }

            if (futureLowestRow > _lastLowestRow)
            {
                int rowsToMove = futureLowestRow - _lastLowestRow;
                MoveGridDown(rowsToMove);
                _lastLowestRow = futureLowestRow;
            }
        }

        private int GetLowestOccupiedRowAfterDestruction(List<Ball> ballsToDestroy)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return -1;

            HashSet<Ball> toDestroy = new HashSet<Ball>(ballsToDestroy);

            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball != null && !toDestroy.Contains(ball))
                    {
                        return row;
                    }
                }
            }

            return -1;
        }

        private int GetHighestOccupiedRow()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return -1;

            for (int row = gridManager.Balls.Count - 1; row >= 0; row--)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball != null)
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

            if (newPosition.y > _initialGridPosition.y)
            {
                newPosition.y = _initialGridPosition.y;
            }

            int highestOccupiedRow = GetHighestOccupiedRow();
            if (highestOccupiedRow != -1)
            {
                Vector2Int topBallGridPos = new(0, highestOccupiedRow);
                Vector3 topBallCurrentWorldPos = GridUtils.PosToWorld(topBallGridPos, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);
                float topBallNewY = topBallCurrentWorldPos.y + (newPosition.y - currentPosition.y);

                if (topBallNewY < _topBoundaryY)
                {
                    float maxAllowedMovement = topBallCurrentWorldPos.y - _topBoundaryY;
                    newPosition.y = currentPosition.y - maxAllowedMovement;
                }
            }

            _gridTween?.Kill();
            _isMoving = true;

            float duration = Mathf.Lerp(0.2f, 0.5f, Mathf.Clamp01((rows - 1) / 4f));

            _gridTween = gridManager.GridContainer
                .DOMove(newPosition, duration)
                .SetEase(_scrollEase)
                .OnComplete(() => _isMoving = false);
        }

        private int GetCurrentLowestOccupiedRow()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.Balls == null) return -1;

            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball != null)
                    {
                        return row;
                    }
                }
            }
            return -1;
        }

        public void ResetScroll()
        {
            _lastLowestRow = -1;
            _gridTween?.Kill();

            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                Vector3 currentPosition = GridManager.Instance.GridContainer.position;
                float distance = Vector3.Distance(currentPosition, _initialGridPosition);
                float duration = Mathf.Max(distance / _scrollSpeed, _minScrollDuration);

                _gridTween = GridManager.Instance.GridContainer
                    .DOMove(_initialGridPosition, duration)
                    .SetEase(_scrollEase);
            }
        }

        public void ResetScrollInstant()
        {
            _lastLowestRow = -1;
            _gridTween?.Kill();

            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null && _initialGridPosition != Vector3.zero)
            {
                GridManager.Instance.GridContainer.position = _initialGridPosition;
            }
        }
    }
}
