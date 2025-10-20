using UnityEngine;

namespace Brain.Util
{
    public static class GridUtils
    {
        // Static Fields - Hexagonal neighbor offset patterns (6 neighbors per cell)
        // Pattern order: top-left, top-right, right, bottom-right, bottom-left, left
        private static readonly int[][] s_CShifts = {
            new int[] { -1, 0, 1, 0, -1, -1 },  // Even row x-offsets (row 0 at bottom)
            new int[] { 0, 1, 1, 1, 0, -1 }     // Odd row x-offsets
        };

        // Y-offsets: inverted because row 0 is at bottom (not top like toolkit)
        private static readonly int[] s_RShifts = { 1, 1, 0, -1, -1, 0 };

        // Public Static Methods
        public static Vector3 PosToWorld(Vector2Int gridPos, float ballWidth, float ballHeight, Transform gridOrigin)
        {
            int maxColumns = GetMaxColumns(gridPos.y);

            // Calculate world position with hexagonal offset
            float worldX = gridPos.x * ballWidth - (maxColumns / 2f * ballWidth);
            float worldY = gridPos.y * ballHeight;

            return gridOrigin.position + new Vector3(worldX, worldY, 0);
        }

        public static Vector2Int WorldToPos(Vector3 worldPos, float ballWidth, float ballHeight, Transform gridOrigin, int maxColumns)
        {
            Vector3 localPos = worldPos - gridOrigin.position;

            // Calculate row (y) - positive Y goes up
            int y = Mathf.RoundToInt(localPos.y / ballHeight);

            // Calculate column (x) with hexagonal offset
            int columnMax = GetMaxColumns(y);
            float offsetX = localPos.x + (columnMax / 2f * ballWidth);
            int x = Mathf.RoundToInt(offsetX / ballWidth);

            // Clamp to valid range
            x = Mathf.Clamp(x, 0, columnMax - 1);

            return new Vector2Int(x, y);
        }

        public static Vector2Int?[] GetNeighborPositions(Vector2Int gridPos, int maxColumns, int maxRows)
        {
            Vector2Int?[] neighbors = new Vector2Int?[6];

            int checkRow = gridPos.y;
            int checkCol = gridPos.x;

            // Select shift pattern based on even/odd row
            int[] currentCShifts = s_CShifts[checkRow % 2];

            for (int i = 0; i < 6; i++)
            {
                int neighborCol = checkCol + currentCShifts[i];
                int neighborRow = checkRow + s_RShifts[i];

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

        public static bool IsValidPosition(int col, int row, int maxColumns, int maxRows)
        {
            if (row < 0 || row >= maxRows)
                return false;

            int columnMax = GetMaxColumns(row);
            if (col < 0 || col >= columnMax)
                return false;

            return true;
        }

        public static int GetMaxColumns(int row)
        {
            // For hexagonal grids, odd rows are typically offset and have one less column
            return row % 2 == 0 ? 11 : 10;
        }

        public static int GetGridDistance(Vector2Int posA, Vector2Int posB)
        {
            return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
        }

        public static Vector2Int FindNearestEmptyCell(Vector3 worldPos, float ballWidth, float ballHeight, Transform gridOrigin, int maxColumns, int maxRows, System.Func<int, int, bool> isCellEmpty)
        {
            Vector2Int centerPos = WorldToPos(worldPos, ballWidth, ballHeight, gridOrigin, maxColumns);

            float minDistance = float.MaxValue;
            Vector2Int bestPos = new Vector2Int(-1, -1);
            bool foundEmpty = false;

            // Expand search radius until we find an empty cell
            for (int radius = 0; radius <= 10 && !foundEmpty; radius++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        // Only check cells at current radius edge
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius)
                            continue;

                        int checkX = centerPos.x + dx;
                        int checkY = centerPos.y + dy;

                        if (!IsValidPosition(checkX, checkY, maxColumns, maxRows))
                            continue;

                        if (!isCellEmpty(checkX, checkY))
                            continue;

                        Vector3 cellWorldPos = PosToWorld(new Vector2Int(checkX, checkY), ballWidth, ballHeight, gridOrigin);
                        float distance = Vector3.Distance(worldPos, cellWorldPos);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            bestPos = new Vector2Int(checkX, checkY);
                            foundEmpty = true;
                        }
                    }
                }
            }

            return bestPos;
        }
    }
}
