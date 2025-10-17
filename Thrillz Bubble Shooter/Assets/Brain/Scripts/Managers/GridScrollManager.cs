using System;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class GridScrollManager : UnitySingleton<GridScrollManager>
    {
        [Header("Scroll Settings")]
        [SerializeField] private int deathLineRow = 0;
        [SerializeField] private int targetBufferRows = 4;

        private Vector3 initialGridPosition;
        private int lastLowestRow = -1;

        public event Action OnDeathLineTouched;

        private void Start()
        {
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                initialGridPosition = GridManager.Instance.GridContainer.position;
            }
        }

        /// <summary>
        /// Checks and updates grid position to maintain buffer
        /// </summary>
        public void UpdateGridPosition()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            int lowestRow = GetLowestOccupiedRow();
            if (lowestRow == -1) return;

            // Check death line collision
            if (lowestRow <= deathLineRow)
            {
                OnDeathLineTouched?.Invoke();
                return;
            }

            // Move grid up if bottom row cleared
            if (lowestRow > lastLowestRow && lastLowestRow != -1)
            {
                int rowsToMove = lowestRow - lastLowestRow;
                MoveGridUp(rowsToMove);
            }

            lastLowestRow = lowestRow;
        }

        /// <summary>
        /// Finds the lowest row with any ball
        /// </summary>
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

        /// <summary>
        /// Moves grid up by specified rows
        /// </summary>
        private void MoveGridUp(int rows)
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || gridManager.GridContainer == null) return;

            float moveDistance = rows * gridManager.BallHeight;
            Vector3 newPosition = gridManager.GridContainer.position + new Vector3(0, -moveDistance, 0);

            // Don't move above initial position (anchor limit)
            if (newPosition.y > initialGridPosition.y)
            {
                newPosition.y = initialGridPosition.y;
            }

            gridManager.GridContainer.position = newPosition;
        }

        /// <summary>
        /// Resets scroll state
        /// </summary>
        public void ResetScroll()
        {
            lastLowestRow = -1;
            if (GridManager.Instance != null && GridManager.Instance.GridContainer != null)
            {
                GridManager.Instance.GridContainer.position = initialGridPosition;
            }
        }
    }
}
