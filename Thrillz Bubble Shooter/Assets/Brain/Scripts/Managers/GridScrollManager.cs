using System;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class GridScrollManager : UnitySingleton<GridScrollManager>
    {
        // Serialized Fields
        [Header("Scroll Settings")]
        [SerializeField] private int _deathLineRow = 0;
        [SerializeField] private int _targetBufferRows = 4;

        // Private Fields
        private Vector3 _initialGridPosition;
        private int _lastLowestRow = -1;

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
            Vector3 newPosition = gridManager.GridContainer.position + new Vector3(0, -moveDistance, 0);

            // Don't move above initial position (anchor limit)
            if (newPosition.y > _initialGridPosition.y)
            {
                newPosition.y = _initialGridPosition.y;
            }

            gridManager.GridContainer.position = newPosition;
        }

        public void ResetScroll()
        {
            _lastLowestRow = -1;
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                GridManager.Instance.GridContainer.position = _initialGridPosition;
            }
        }
    }
}
