using Brain.Gameplay;
using Brain.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Brain.Managers
{
    public class LevelGenerator : UnitySingleton<LevelGenerator>
    {
        [Header("Generation Settings")]
        [SerializeField] private int totalRows = 60;
        [SerializeField] private int startRow = 4;
        [SerializeField][Range(0f, 1f)] private float fillRate = 0.8f;
        [SerializeField] private bool removeOrphans = true;

        /// <summary>
        /// Generates procedural level layout using game seed
        /// </summary>
        public void GenerateLevel(int seed)
        {
            Random.InitState(seed);

            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found!");
                return;
            }

            int endRow = startRow + totalRows;
            int ballsGenerated = 0;

            Debug.Log($"LevelGenerator: Generating rows {startRow} to {endRow - 1} (total: {totalRows} rows)");

            for (int row = startRow; row < endRow; row++)
            {
                int columnsInRow = GridUtils.GetMaxColumns(row);

                for (int col = 0; col < columnsInRow; col++)
                {
                    if (Random.value < fillRate)
                    {
                        BallColor randomColor = (BallColor)Random.Range(0, 6);
                        gridManager.SpawnBall(col, row, randomColor);
                        ballsGenerated++;
                    }
                }
            }

            Debug.Log($"LevelGenerator: Generated {ballsGenerated} balls");

            // Update neighbors BEFORE doing any orphan detection
            gridManager.FinalizeGrid();

            // Mark only the HIGHEST row as ceiling (like toolkit does with y==0)
            MarkCeilingBalls();

            // Remove orphaned balls from generation
            if (removeOrphans)
            {
                RemoveOrphanedBalls();
            }
        }

        /// <summary>
        /// Marks only the highest row with balls as ceiling
        /// </summary>
        private void MarkCeilingBalls()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            // Find highest row with any ball
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

            // Mark ONLY the highest row as ceiling/root (like toolkit marks y==0)
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

        /// <summary>
        /// Removes balls that aren't connected to ceiling
        /// </summary>
        private void RemoveOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null) return;

            gridManager.ClearAllMarks();

            // Find all connected balls starting from root balls
            HashSet<Ball> connectedBalls = new HashSet<Ball>();
            foreach (Ball rootBall in Ball.RootBalls)
            {
                FindConnectedBalls(rootBall, connectedBalls);
            }

            // Remove balls that aren't connected
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
                Destroy(ball.gameObject);
            }

            gridManager.ClearAllMarks();
        }

        /// <summary>
        /// Flood-fill to find all connected balls
        /// </summary>
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
    }
}
