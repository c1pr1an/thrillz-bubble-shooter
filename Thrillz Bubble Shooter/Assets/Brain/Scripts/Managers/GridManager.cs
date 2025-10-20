using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class GridManager : UnitySingleton<GridManager>
    {
        // Private Fields
        [Header("Grid Settings")]
        [SerializeField] private int _maxColumns = 11;
        [SerializeField] private int _maxRows = 66;
        [SerializeField] private float _ballWidth = 1f;
        [SerializeField] private float _ballHeight = 0.87f;

        [SerializeField] private Ball[] _ballPrefabs = new Ball[6];

        [Header("Grid Container")]
        [SerializeField] private Transform _gridContainer;

        // 2D grid matrix [row][column] - matches toolkit's structure
        private List<List<Ball>> _balls;

        // Properties
        public List<List<Ball>> Balls => _balls;
        public int MaxColumns => _maxColumns;
        public int MaxRows => _maxRows;
        public float BallWidth => _ballWidth;
        public float BallHeight => _ballHeight;
        public Transform GridContainer => _gridContainer;

        /// <summary>
        /// Gets a ball prefab by color index (single source of truth)
        /// </summary>
        public Ball GetBallPrefab(int colorIndex)
        {
            if (colorIndex < 0 || colorIndex >= _ballPrefabs.Length)
            {
                Debug.LogError($"GridManager: Invalid ball color index {colorIndex}");
                return null;
            }

            return _ballPrefabs[colorIndex];
        }

        /// <summary>
        /// Initializes the grid structure
        /// </summary>
        public void InitializeGrid()
        {
            _balls = new List<List<Ball>>(_maxRows);

            for (int row = 0; row < _maxRows; row++)
            {
                int columnsInRow = GridUtils.GetMaxColumns(row);
                List<Ball> rowList = new List<Ball>(columnsInRow);

                for (int col = 0; col < columnsInRow; col++)
                {
                    rowList.Add(null);
                }

                _balls.Add(rowList);
            }
        }

        /// <summary>
        /// Finalizes grid setup after _balls are spawned
        /// </summary>
        public void FinalizeGrid()
        {
            UpdateAllNeighbors();
        }

        /// <summary>
        /// Spawns a ball at the given grid position
        /// </summary>
        public Ball SpawnBall(int col, int row, BallColor color)
        {
            if (!GridUtils.IsValidPosition(col, row, _maxColumns, _maxRows))
            {
                Debug.LogWarning($"Invalid grid position: ({col}, {row})");
                return null;
            }

            // Get the correct prefab for this color
            int colorIndex = (int)color;
            if (colorIndex < 0 || colorIndex >= _ballPrefabs.Length || _ballPrefabs[colorIndex] == null)
            {
                Debug.LogError($"Ball prefab for color {color} (index {colorIndex}) is not assigned!");
                return null;
            }

            // Calculate world position
            Vector2Int gridPos = new Vector2Int(col, row);
            Vector3 worldPos = GridUtils.PosToWorld(gridPos, _ballWidth, _ballHeight, _gridContainer);

            // Instantiate the correct ball prefab
            Ball ball = Instantiate(_ballPrefabs[colorIndex], worldPos, Quaternion.identity, _gridContainer);
            ball.name = $"Ball_{color}_{row}_{col}";
            ball.SetColor(color);
            ball.SetPosition(gridPos, worldPos);

            // Add to grid
            _balls[row][col] = ball;

            return ball;
        }

        /// <summary>
        /// Gets the exact grid snap position for a given world position
        /// </summary>
        public Vector3 GetGridSnapPosition(Vector3 worldPosition)
        {
            // Find nearest empty cell using distance-based search
            Vector2Int gridPos = GridUtils.FindNearestEmptyCell(
                worldPosition,
                _ballWidth,
                _ballHeight,
                _gridContainer,
                _maxColumns,
                _maxRows,
                (x, y) => GetBall(x, y) == null
            );

            if (gridPos.x < 0 || gridPos.y < 0)
            {
                // Fallback to just returning the original position if no valid grid spot
                return worldPosition;
            }

            // Convert grid position to world position (this is the exact snap position)
            return GridUtils.PosToWorld(gridPos, _ballWidth, _ballHeight, _gridContainer);
        }

        /// <summary>
        /// Adds a launched ball to the grid at the closest valid position
        /// </summary>
        public void AddBallToGrid(Ball ball, Vector3 worldPosition)
        {
            // Find nearest empty cell using distance-based search
            Vector2Int gridPos = GridUtils.FindNearestEmptyCell(
                worldPosition,
                _ballWidth,
                _ballHeight,
                _gridContainer,
                _maxColumns,
                _maxRows,
                (x, y) => GetBall(x, y) == null
            );

            if (gridPos.x < 0 || gridPos.y < 0)
            {
                Debug.LogError("Could not find empty grid position for ball!");
                Destroy(ball.gameObject);
                return;
            }

            // Parent to grid container so it moves with the grid
            ball.transform.SetParent(_gridContainer);

            // Snap to grid world position
            Vector3 snappedWorldPos = GridUtils.PosToWorld(gridPos, _ballWidth, _ballHeight, _gridContainer);
            ball.transform.position = snappedWorldPos;
            ball.SetPosition(gridPos, snappedWorldPos);

            // Add to grid matrix
            _balls[gridPos.y][gridPos.x] = ball;

            // Update neighbors
            UpdateNeighbors(ball);
            UpdateAdjacentNeighbors(ball);
        }

        /// <summary>
        /// Removes a ball from the grid
        /// </summary>
        public void RemoveBall(Ball ball)
        {
            if (ball == null) return;

            Vector2Int pos = ball.Position;
            if (pos.y >= 0 && pos.y < _balls.Count && pos.x >= 0 && pos.x < _balls[pos.y].Count)
            {
                if (_balls[pos.y][pos.x] == ball)
                {
                    _balls[pos.y][pos.x] = null;
                }
            }

            // Update neighbors of adjacent _balls
            UpdateAdjacentNeighbors(ball);
        }

        /// <summary>
        /// Updates neighbor references for a single ball
        /// </summary>
        private void UpdateNeighbors(Ball ball)
        {
            if (ball == null) return;

            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(ball.Position, _maxColumns, _maxRows);
            Ball[] neighbors = new Ball[6];

            for (int i = 0; i < 6; i++)
            {
                if (neighborPositions[i].HasValue)
                {
                    Vector2Int neighborPos = neighborPositions[i].Value;
                    neighbors[i] = GetBall(neighborPos.x, neighborPos.y);
                }
                else
                {
                    neighbors[i] = null;
                }
            }

            ball.UpdateNeighbors(neighbors);
        }

        /// <summary>
        /// Updates neighbor references for all _balls adjacent to the given ball
        /// </summary>
        private void UpdateAdjacentNeighbors(Ball ball)
        {
            if (ball == null) return;

            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(ball.Position, _maxColumns, _maxRows);

            foreach (var neighborPos in neighborPositions)
            {
                if (neighborPos.HasValue)
                {
                    Ball neighbor = GetBall(neighborPos.Value.x, neighborPos.Value.y);
                    if (neighbor != null)
                    {
                        UpdateNeighbors(neighbor);
                    }
                }
            }
        }

        /// <summary>
        /// Updates all ball neighbor references
        /// </summary>
        private void UpdateAllNeighbors()
        {
            for (int row = 0; row < _balls.Count; row++)
            {
                for (int col = 0; col < _balls[row].Count; col++)
                {
                    Ball ball = _balls[row][col];
                    if (ball != null)
                    {
                        UpdateNeighbors(ball);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a ball at the given grid position
        /// </summary>
        public Ball GetBall(int col, int row)
        {
            if (row < 0 || row >= _balls.Count || col < 0 || col >= _balls[row].Count)
            {
                return null;
            }

            return _balls[row][col];
        }

        /// <summary>
        /// Clears all grid marks (for match detection and orphan detection)
        /// </summary>
        public void ClearAllMarks()
        {
            for (int row = 0; row < _balls.Count; row++)
            {
                for (int col = 0; col < _balls[row].Count; col++)
                {
                    Ball ball = _balls[row][col];
                    if (ball != null)
                    {
                        ball.Flags &= ~BallFlags.MarkConnected;
                        ball.Flags &= ~BallFlags.MarkedForMatch;
                        ball.Flags &= ~BallFlags.MarkedForDestroy;
                    }
                }
            }
        }
    }
}
