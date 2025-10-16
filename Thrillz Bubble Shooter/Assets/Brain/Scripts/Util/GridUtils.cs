using UnityEngine;

namespace Brain.Util
{
    /// <summary>
    /// Hexagonal grid utilities for positioning and neighbor calculations
    /// </summary>
    public static class GridUtils
    {
        // Hexagonal neighbor offset patterns (6 neighbors per cell)
        // Even rows and odd rows have different x-offsets due to hexagonal stagger
        private static readonly int[][] cShifts = {
            new int[] { 0, 1, 1, 1, 0, -1 },   // Even row x-offsets
            new int[] { -1, 0, 1, 0, -1, -1 }  // Odd row x-offsets
        };

        // Y-offsets are the same for both even and odd rows
        private static readonly int[] rShifts = { -1, -1, 0, 1, 1, 0 };

        /// <summary>
        /// Converts grid position to world position
        /// </summary>
        public static Vector3 PosToWorld(Vector2Int gridPos, float ballWidth, float ballHeight, Transform gridOrigin)
        {
            int maxColumns = GetMaxColumns(gridPos.y);

            // Calculate world position with hexagonal offset
            float worldX = gridPos.x * ballWidth - (maxColumns / 2f * ballWidth);
            float worldY = -gridPos.y * ballHeight;

            return gridOrigin.position + new Vector3(worldX, worldY, 0);
        }

        /// <summary>
        /// Converts world position to grid position
        /// </summary>
        public static Vector2Int WorldToPos(Vector3 worldPos, float ballWidth, float ballHeight, Transform gridOrigin, int maxColumns)
        {
            Vector3 localPos = worldPos - gridOrigin.position;

            // Calculate row (y)
            int y = Mathf.RoundToInt(Mathf.Abs(localPos.y) / ballHeight);

            // Calculate column (x) with hexagonal offset
            int columnMax = GetMaxColumns(y);
            float offsetX = localPos.x + (columnMax / 2f * ballWidth);
            int x = Mathf.RoundToInt(offsetX / ballWidth);

            // Clamp to valid range
            x = Mathf.Clamp(x, 0, columnMax - 1);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Gets the 6 hexagonal neighbors for a ball at given position
        /// Returns array of neighbor grid positions (or null for out of bounds)
        /// </summary>
        public static Vector2Int?[] GetNeighborPositions(Vector2Int gridPos, int maxColumns, int maxRows)
        {
            Vector2Int?[] neighbors = new Vector2Int?[6];

            int checkRow = gridPos.y;
            int checkCol = gridPos.x;

            // Select shift pattern based on even/odd row
            int[] currentCShifts = cShifts[checkRow % 2];

            for (int i = 0; i < 6; i++)
            {
                int neighborCol = checkCol + currentCShifts[i];
                int neighborRow = checkRow + rShifts[i];

                // Check if neighbor is within valid grid bounds
                if (IsValidPosition(neighborCol, neighborRow, maxColumns, maxRows))
                {
                    neighbors[i] = new Vector2Int(neighborCol, neighborRow);
                }
                else
                {
                    neighbors[i] = null;
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Checks if a grid position is valid (within bounds)
        /// </summary>
        public static bool IsValidPosition(int col, int row, int maxColumns, int maxRows)
        {
            if (row < 0 || row >= maxRows)
                return false;

            int columnMax = GetMaxColumns(row);
            if (col < 0 || col >= columnMax)
                return false;

            return true;
        }

        /// <summary>
        /// Gets maximum columns for a given row (accounts for hexagonal stagger)
        /// Even rows have full width, odd rows have one less column
        /// </summary>
        public static int GetMaxColumns(int row)
        {
            // For hexagonal grids, odd rows are typically offset and have one less column
            // This matches the toolkit's implementation
            return row % 2 == 0 ? 11 : 10;
        }

        /// <summary>
        /// Calculates distance between two grid positions (Manhattan distance)
        /// </summary>
        public static int GetGridDistance(Vector2Int posA, Vector2Int posB)
        {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        }
    }
}
