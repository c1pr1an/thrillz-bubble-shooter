using System.Collections.Generic;
using Brain.Audio;
using Brain.Gameplay;
using Brain.Gameplay.Containers;
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

        [Header("Grid Container")]
        [SerializeField] private Transform _gridContainer;
        [SerializeField] private Transform _limitLine;
        [SerializeField] private Transform _limitLineRed;
        [SerializeField] private ParticleSystem _limitHitBallVFX;
        [SerializeField] private BallPreviewContainer _ballPreviewContainer;
        [SerializeField] private LaunchContainer _ballLaunchContainer;
        [SerializeField] private BonusBallContainer _bonusBallContainer;

        // 2D grid matrix [row][column] 
        private List<List<Ball>> _balls;

        // Properties
        public List<List<Ball>> Balls => _balls;
        public int MaxColumns => _maxColumns;
        public int MaxRows => _maxRows;
        public float BallWidth => _ballWidth;
        public float BallHeight => _ballHeight;
        public Transform GridContainer => _gridContainer;
        public Transform LimitLine => _limitLine;
        public Transform LimitLineRed => _limitLineRed;
        public ParticleSystem LimitHitBallVFX => _limitHitBallVFX;
        public BallPreviewContainer BallPreviewContainer => _ballPreviewContainer;
        public LaunchContainer BallLaunchContainer => _ballLaunchContainer;
        public BonusBallContainer BonusBallContainer => _bonusBallContainer;

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

            _ballPreviewContainer.Init(_ballLaunchContainer, _bonusBallContainer);
            _ballLaunchContainer.Init(_ballPreviewContainer, _bonusBallContainer);
            _bonusBallContainer.Init(_ballLaunchContainer);
            InputManager.Instance.Init(_ballPreviewContainer, _bonusBallContainer);
        }

        public void FinalizeGrid()
        {
            UpdateAllNeighbors();

            PhantomBallManager.Instance.InitializePhantoms();
        }

        public Ball SpawnBall(int col, int row, BallColor color)
        {
            if (!GridUtils.IsValidPosition(col, row))
            {
                Debug.LogWarning($"Invalid grid position: ({col}, {row})");
                return null;
            }

            // Get ball from pool
            GameObject ballObj = ObjectPooler.Instance.Get(color);
            if (ballObj == null)
            {
                Debug.LogError($"Failed to get ball from pool for color {color}");
                return null;
            }

            Ball ball = ballObj.GetComponent<Ball>();
            if (ball == null)
            {
                Debug.LogError($"Pooled object doesn't have Ball component for color {color}");
                return null;
            }

            // Calculate world position
            Vector2Int gridPos = new Vector2Int(col, row);
            Vector3 worldPos = GridUtils.PosToWorld(gridPos, _ballWidth, _ballHeight, _gridContainer);

            // Configure the ball
            ball.transform.position = worldPos;
            ball.transform.rotation = Quaternion.identity;
            ball.transform.SetParent(_gridContainer);
            ball.name = $"Ball_{color}_{row}_{col}";
            ball.SetColor(color);
            ball.SetPosition(gridPos, worldPos);

            // Add to grid
            _balls[row][col] = ball;

            // Track color in ColorTracker (only for normal balls, not bonus)
            if (!ball.IsBonusBall)
            {
                ColorTrackerManager.Instance.AddColor(color);
            }

            return ball;
        }

        public Vector3 GetGridSnapPosition(Vector3 worldPosition)
        {
            // Find nearest empty cell using distance-based search
            Vector2Int gridPos = GridUtils.FindNearestEmptyCell(
                worldPosition,
                _ballWidth,
                _ballHeight,
                _gridContainer,
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
            // Special handling for rocket balls - they don't snap to grid
            if (ball.IsRocket())
            {
                // Parent to grid container so it moves with the grid
                ball.transform.SetParent(_gridContainer);

                // Keep rocket at its exact position
                ball.transform.position = worldPosition;

                // Set a dummy grid position for consistency (used for debugging)
                Vector2Int approximateGridPos = GridUtils.WorldToPos(worldPosition, _ballWidth, _ballHeight, _gridContainer);
                ball.SetPosition(approximateGridPos, worldPosition);

                // Mark as pinned so it can be properly destroyed
                ball.Flags |= BallFlags.Pinned;

                // Don't add to grid matrix or update neighbors - rocket exists outside the grid
                return;
            }

            // Normal ball handling - find nearest empty cell
            Vector2Int gridPos = GridUtils.FindNearestEmptyCell(
                worldPosition,
                _ballWidth,
                _ballHeight,
                _gridContainer,
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

            // Track color in ColorTracker (only for normal balls, not bonus/rocket)
            if (!ball.IsBonusBall && !ball.IsRocket())
            {
                ColorTrackerManager.Instance.AddColor(ball.Color);
            }

            UpdateNeighbors(ball);
            UpdateAdjacentNeighbors(ball);

            // Trigger wave effect after ball is placed
            WaveEffectManager.Instance.TriggerWaveEffect(ball);
            PhantomBallManager.Instance.OnBallAddedToGrid(ball);
            SoundManager.Instance.PlaySfxOneShot(SoundType.Game_BallGridImpact);
        }

        public void RemoveBall(Ball ball)
        {
            if (ball == null) return;

            PhantomBallManager.Instance.OnBallRemovedFromGrid(ball);

            Vector2Int pos = ball.GridPosition;
            if (pos.y >= 0 && pos.y < _balls.Count && pos.x >= 0 && pos.x < _balls[pos.y].Count)
            {
                if (_balls[pos.y][pos.x] == ball)
                {
                    _balls[pos.y][pos.x] = null;

                    // Remove color from ColorTracker (only for normal balls, not bonus)
                    if (!ball.IsBonusBall)
                    {
                        ColorTrackerManager.Instance.RemoveColor(ball.Color);
                    }
                }
            }

            UpdateAdjacentNeighbors(ball);
        }

        /// <summary>
        /// Updates neighbor references for a single ball
        /// </summary>
        private void UpdateNeighbors(Ball ball)
        {
            if (ball == null) return;

            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(ball.GridPosition);
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

            Vector2Int?[] neighborPositions = GridUtils.GetNeighborPositions(ball.GridPosition);

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
        /// Clears temporary grid marks used for detection algorithms
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
                    }
                }
            }
        }
    }
}
