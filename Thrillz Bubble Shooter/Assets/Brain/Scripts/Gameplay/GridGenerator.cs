using Brain.Managers;
using Brain.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Gameplay
{
    public class GridGenerator : UnitySingleton<GridGenerator>
    {
        [Header("Grid Settings")]
        [SerializeField] private int _totalRows = 60;
        [SerializeField] private int _startRow = 4;
        [SerializeField][Range(0.6f, 0.9f)] private float _fillRate = 0.75f;
        [SerializeField][Range(0.4f, 0.8f)] private float _clusteringStrength = 0.65f;

        [Header("Color Balance")]
        [SerializeField][Range(0.3f, 0.7f)] private float _neighborColorWeight = 0.55f;
        [SerializeField][Range(0.3f, 0.7f)] private float _randomColorWeight = 0.45f;
        [SerializeField][Range(1, 3)] private int _minMatchableNeighbors = 2;

        // Constants
        private const int COLOR_COUNT = 6;
        private const int MIN_CLUSTER_SIZE = 3;

        public void GenerateGrid()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found!");
                return;
            }

            int endRow = _startRow + _totalRows;
            int ballsGenerated = 0;

            Debug.Log($"GridGenerator: Generating {_totalRows} rows, fill rate: {_fillRate:F2}");

            ballsGenerated = GenerateMixedGrid(gridManager, _startRow, endRow);

            Debug.Log($"GridGenerator: Initial generation created {ballsGenerated} balls");

            gridManager.FinalizeGrid();

            EnhanceClusters(gridManager);
            RemoveOrConvertIsolatedBalls(gridManager);
            MarkCeilingBalls();
            RemoveOrphanedBalls();

            // Validate level is solvable
            bool isSolvable = ValidateLevelSolvability(gridManager);

            Debug.Log($"GridGenerator: Final grid has {CountBalls()} balls - Solvable: {isSolvable}");
        }

        private void MarkCeilingBalls()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            int highestRow = -1;
            for (int row = gridManager.Balls.Count - 1; row >= 0; row--)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    if (gridManager.Balls[row][col] != null)
                    {
                        highestRow = row;
                        break;
                    }
                }
                if (highestRow != -1) break;
            }

            if (highestRow == -1) return;

            for (int col = 0; col < gridManager.Balls[highestRow].Count; col++)
            {
                Ball ball = gridManager.Balls[highestRow][col];
                if (ball != null)
                {
                    ball.Flags |= BallFlags.Root;
                }
            }
        }

        private int CountBalls()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return 0;

            int count = 0;
            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    if (gridManager.Balls[row][col] != null)
                        count++;
                }
            }
            return count;
        }

        private void RemoveOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            gridManager.ClearAllMarks();

            HashSet<Ball> connectedBalls = new HashSet<Ball>();
            foreach (Ball rootBall in Ball.s_rootBalls)
            {
                FindConnectedBalls(rootBall, connectedBalls);
            }

            List<Ball> ballsToRemove = new List<Ball>();
            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball != null && !connectedBalls.Contains(ball))
                    {
                        ballsToRemove.Add(ball);
                    }
                }
            }

            foreach (Ball ball in ballsToRemove)
            {
                gridManager.RemoveBall(ball);
                ball.ReturnToPoolInstantly();
            }

            gridManager.ClearAllMarks();
        }

        private void FindConnectedBalls(Ball ball, HashSet<Ball> connectedBalls)
        {
            if (ball == null || connectedBalls.Contains(ball)) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;

            connectedBalls.Add(ball);

            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    FindConnectedBalls(neighbor, connectedBalls);
                }
            }
        }

        private int GenerateMixedGrid(GridManager gridManager, int startRow, int endRow)
        {
            // Mix weighted and cluster approaches for best results
            int ballsGenerated = 0;
            int sectionHeight = _totalRows / 3;

            for (int row = startRow; row < endRow; row++)
            {
                int columnsInRow = GridUtils.GetMaxColumns(row);
                int sectionIndex = (row - startRow) / sectionHeight;

                for (int col = 0; col < columnsInRow; col++)
                {
                    if (Random.value < _fillRate)
                    {
                        BallColor color;

                        // Alternate between patterns based on section
                        if (sectionIndex % 2 == 0)
                        {
                            color = GetWeightedColor(gridManager, col, row);
                        }
                        else
                        {
                            // Use cluster-like generation
                            float noise = Mathf.PerlinNoise(col * 0.3f, row * 0.3f);
                            int colorIndex = Mathf.FloorToInt(noise * COLOR_COUNT) % COLOR_COUNT;
                            color = (BallColor)colorIndex;
                        }

                        gridManager.SpawnBall(col, row, color);
                        ballsGenerated++;
                    }
                }
            }

            return ballsGenerated;
        }

        #region Color Selection Helpers

        private BallColor GetWeightedColor(GridManager gridManager, int col, int row)
        {
            // Get neighboring colors
            Dictionary<BallColor, float> colorWeights = new();

            // Initialize all colors with base weight
            for (int i = 0; i < COLOR_COUNT; i++)
            {
                colorWeights[(BallColor)i] = _randomColorWeight;
            }

            // Check neighbors and increase weight for neighboring colors
            List<Ball> neighbors = GetPotentialNeighbors(gridManager, col, row);
            foreach (Ball neighbor in neighbors)
            {
                if (neighbor != null)
                {
                    colorWeights[neighbor.Color] += _neighborColorWeight;
                }
            }

            // Select color based on weights
            return SelectWeightedColor(colorWeights);
        }

        private BallColor GetColorBasedOnClusterDistance(Dictionary<BallColor, Vector2> clusterCenters, int col, int row)
        {
            BallColor closestColor = BallColor.Red;
            float minDistance = float.MaxValue;

            foreach (var kvp in clusterCenters)
            {
                float distance = Vector2.Distance(new Vector2(col, row), kvp.Value);

                // Add some randomness to avoid perfect circles
                distance += Random.Range(-_clusteringStrength * 2, _clusteringStrength * 2);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestColor = kvp.Key;
                }
            }

            return closestColor;
        }

        private BallColor GetGradientColor(float rowProgress, float colProgress)
        {
            // Create diagonal gradient
            float gradientValue = (rowProgress + colProgress) / 2f;
            gradientValue += Random.Range(-0.1f, 0.1f); // Add noise

            int colorIndex = Mathf.FloorToInt(gradientValue * COLOR_COUNT);
            colorIndex = Mathf.Clamp(colorIndex, 0, COLOR_COUNT - 1);

            return (BallColor)colorIndex;
        }

        private BallColor GetIslandColor(List<IslandData> islands, int col, int row)
        {
            Vector2 position = new Vector2(col, row);
            float totalWeight = 0f;
            Dictionary<BallColor, float> colorWeights = new();

            foreach (var island in islands)
            {
                float distance = Vector2.Distance(position, island.center);
                if (distance <= island.radius)
                {
                    float weight = 1f - (distance / island.radius);
                    weight = Mathf.Pow(weight, 2); // Stronger falloff

                    if (!colorWeights.ContainsKey(island.color))
                        colorWeights[island.color] = 0;

                    colorWeights[island.color] += weight;
                    totalWeight += weight;
                }
            }

            if (totalWeight > 0)
            {
                return SelectWeightedColor(colorWeights);
            }

            // If outside all islands, return random color
            return (BallColor)Random.Range(0, COLOR_COUNT);
        }

        private BallColor SelectWeightedColor(Dictionary<BallColor, float> weights)
        {
            float totalWeight = 0;
            foreach (var weight in weights.Values)
                totalWeight += weight;

            float randomValue = Random.Range(0, totalWeight);
            float currentWeight = 0;

            foreach (var kvp in weights)
            {
                currentWeight += kvp.Value;
                if (randomValue <= currentWeight)
                    return kvp.Key;
            }

            return (BallColor)Random.Range(0, COLOR_COUNT);
        }

        private List<Ball> GetPotentialNeighbors(GridManager gridManager, int col, int row)
        {
            List<Ball> neighbors = new();

            // Get all 6 potential neighbor positions using GridUtils
            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(
                new Vector2Int(col, row),
                GridUtils.GetMaxColumns(row),
                _totalRows + _startRow
            );

            foreach (Vector2Int? pos in neighborPositions)
            {
                if (pos.HasValue)
                {
                    Ball neighbor = gridManager.GetBall(pos.Value.x, pos.Value.y);
                    if (neighbor != null)
                    {
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }

        #endregion

        #region Post Processing

        private void EnhanceClusters(GridManager gridManager)
        {
            int enhancements = 0;

            // Find small clusters and expand them
            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball == null) continue;

                    // Count same-color neighbors
                    int sameColorCount = CountSameColorNeighbors(ball);

                    // If ball has 1-2 same color neighbors, try to add more nearby
                    if (sameColorCount > 0 && sameColorCount < MIN_CLUSTER_SIZE)
                    {
                        if (TryEnhanceCluster(gridManager, ball))
                            enhancements++;
                    }
                }
            }

            Debug.Log($"Enhanced {enhancements} clusters");
        }

        private void RemoveOrConvertIsolatedBalls(GridManager gridManager)
        {
            int conversions = 0;
            List<Ball> ballsToProcess = new();

            // Find isolated balls
            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball == null) continue;

                    int sameColorNeighbors = CountSameColorNeighbors(ball);

                    // Ball is isolated if it has no same-color neighbors
                    if (sameColorNeighbors == 0)
                    {
                        ballsToProcess.Add(ball);
                    }
                }
            }

            // Convert isolated balls to nearby colors
            // Note: ConvertToNearbyColor now properly destroys and respawns balls
            foreach (Ball ball in ballsToProcess)
            {
                // Check if ball still exists (it might have been destroyed in previous iteration)
                if (ball != null && ConvertToNearbyColor(ball))
                    conversions++;
            }

            // After conversions, update all neighbors since we've spawned new balls
            if (conversions > 0)
            {
                gridManager.FinalizeGrid();
            }

            Debug.Log($"Converted {conversions} isolated balls");
        }

        private int CountSameColorNeighbors(Ball ball)
        {
            int count = 0;
            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null && neighbor.Color == ball.Color)
                    count++;
            }
            return count;
        }

        private bool TryEnhanceCluster(GridManager gridManager, Ball centerBall)
        {
            // Get all 6 potential neighbor positions using GridUtils
            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(
                centerBall.Position,
                GridUtils.GetMaxColumns(centerBall.Position.y),
                _totalRows + _startRow
            );

            foreach (Vector2Int? pos in neighborPositions)
            {
                if (pos.HasValue && gridManager.GetBall(pos.Value.x, pos.Value.y) == null)
                {
                    // Check if adding a ball here would create a good cluster
                    if (Random.value < _clusteringStrength)
                    {
                        gridManager.SpawnBall(pos.Value.x, pos.Value.y, centerBall.Color);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ConvertToNearbyColor(Ball ball)
        {
            Dictionary<BallColor, int> neighborColors = new();

            // Count neighbor colors
            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    if (!neighborColors.ContainsKey(neighbor.Color))
                        neighborColors[neighbor.Color] = 0;
                    neighborColors[neighbor.Color]++;
                }
            }

            if (neighborColors.Count > 0)
            {
                // Find most common neighbor color
                BallColor mostCommonColor = ball.Color;
                int maxCount = 0;

                foreach (var kvp in neighborColors)
                {
                    if (kvp.Value > maxCount)
                    {
                        maxCount = kvp.Value;
                        mostCommonColor = kvp.Key;
                    }
                }

                if (maxCount >= _minMatchableNeighbors && mostCommonColor != ball.Color)
                {
                    // Store position before returning to pool
                    Vector2Int position = ball.Position;

                    // Remove from grid and return to pool
                    GridManager gridManager = GridManager.Instance;
                    gridManager.RemoveBall(ball);

                    // Return ball to pool instantly (no animation during generation)
                    ball.ReturnToPoolInstantly();

                    // Spawn a new ball with the correct color
                    gridManager.SpawnBall(position.x, position.y, mostCommonColor);

                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Validation

        public bool ValidateLevelSolvability(GridManager gridManager)
        {
            // Basic solvability check - ensure there are matchable groups
            int matchableGroups = 0;
            HashSet<Ball> checkedBalls = new();

            for (int row = 0; row < gridManager.Balls.Count; row++)
            {
                for (int col = 0; col < gridManager.Balls[row].Count; col++)
                {
                    Ball ball = gridManager.Balls[row][col];
                    if (ball == null || checkedBalls.Contains(ball)) continue;

                    // Find connected same-color balls
                    List<Ball> cluster = new();
                    FindColorCluster(ball, cluster, checkedBalls);

                    if (cluster.Count >= 3)
                    {
                        matchableGroups++;
                    }
                }
            }

            bool isSolvable = matchableGroups >= 3; // At least 3 matchable groups

            if (!isSolvable)
            {
                Debug.LogWarning($"Level may not be solvable! Only {matchableGroups} matchable groups found.");
            }
            else
            {
                Debug.Log($"Level validated: {matchableGroups} matchable groups found.");
            }

            return isSolvable;
        }

        private void FindColorCluster(Ball startBall, List<Ball> cluster, HashSet<Ball> checkedBalls)
        {
            if (startBall == null || checkedBalls.Contains(startBall)) return;

            checkedBalls.Add(startBall);
            cluster.Add(startBall);

            foreach (Ball neighbor in startBall.Neighbors)
            {
                if (neighbor != null && neighbor.Color == startBall.Color && !checkedBalls.Contains(neighbor))
                {
                    FindColorCluster(neighbor, cluster, checkedBalls);
                }
            }
        }

        #endregion

        private struct IslandData
        {
            public BallColor color;
            public Vector2 center;
            public float radius;
        }
    }
}
