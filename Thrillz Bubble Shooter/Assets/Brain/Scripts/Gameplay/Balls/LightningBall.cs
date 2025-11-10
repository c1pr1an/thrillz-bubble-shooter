using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Lightning bonus ball.
    /// Lightning balls destroy all balls horizontally (4 left and 4 right of impact).
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class LightningBall : MonoBehaviour, IBonusBall
    {
        private Ball _ball;

        [Header("Visual Settings")]
        [SerializeField] private Transform _lightningParticlesIdle;
        [SerializeField] private bool _enableElectricEffect = true;
        [SerializeField] private float _sparkInterval = 0.5f;
        [SerializeField] private float _glowIntensity = 1.5f;

        [Header("Lightning Settings")]
        [SerializeField] private int _horizontalRange = 4; // Balls to destroy left and right

        private SpriteRenderer _spriteRenderer;
        private float _sparkTimer = 0f;
        private float _glowTimer = 0f;

        private void Awake()
        {
            _ball = GetComponent<Ball>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            // Optional electric visual effect
            if (_enableElectricEffect && _spriteRenderer != null)
            {
                // Glow effect
                _glowTimer += Time.deltaTime * 3f;
                float glow = 1f + Mathf.Sin(_glowTimer) * 0.2f * _glowIntensity;

                // Spark effect (periodic brightness spike)
                _sparkTimer += Time.deltaTime;
                if (_sparkTimer > _sparkInterval)
                {
                    _sparkTimer = 0f;
                    glow = _glowIntensity * 2f; // Brief flash
                }

                // Apply yellow-white electric color with glow
                Color electricColor = Color.Lerp(Color.yellow, Color.white, glow - 1f);
                electricColor *= glow;
                _spriteRenderer.color = electricColor;
            }
        }

        /// <summary>
        /// Check if this is a lightning ball
        /// </summary>
        public bool IsLightning()
        {
            return enabled && _ball != null;
        }

        /// <summary>
        /// Get the horizontal range (how many balls left and right)
        /// </summary>
        public int GetHorizontalRange()
        {
            return _horizontalRange;
        }

        /// <summary>
        /// Set whether the lightning ball is in the launcher container
        /// </summary>
        public void SetInLauncher(bool inLauncher)
        {
            if (_lightningParticlesIdle != null)
            {
                _lightningParticlesIdle.gameObject.SetActive(inLauncher);
            }
        }

        #region IBonusBallDetector Implementation

        /// <summary>
        /// Get all balls that would be affected by the lightning strike (horizontal line)
        /// </summary>
        public List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            List<Ball> affectedBalls = new List<Ball>();

            // Get grid manager
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || _ball == null)
                return affectedBalls;

            // Get the lightning ball's grid position
            // If impact position is provided and different from ball position, use that (for preview)
            Vector2Int lightningPos;
            if (impactPosition != Vector2.zero && (Vector2)_ball.transform.position != impactPosition)
            {
                // Convert impact position to grid position for preview
                lightningPos = GridUtils.WorldToPos(impactPosition, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);
            }
            else
            {
                // Use ball's actual grid position (for actual detection after landing)
                lightningPos = _ball.GridPosition;
            }

            // Collect balls to the left
            for (int x = lightningPos.x - 1; x >= lightningPos.x - _horizontalRange; x--)
            {
                if (!GridUtils.IsValidPosition(x, lightningPos.y))
                    break;

                Ball ballAtPos = gridManager.GetBall(x, lightningPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    affectedBalls.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Collect balls to the right
            for (int x = lightningPos.x + 1; x <= lightningPos.x + _horizontalRange; x++)
            {
                if (!GridUtils.IsValidPosition(x, lightningPos.y))
                    break;

                Ball ballAtPos = gridManager.GetBall(x, lightningPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    affectedBalls.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Add the lightning ball itself to be destroyed
            if (!affectedBalls.Contains(_ball))
            {
                affectedBalls.Add(_ball);
                _ball.Flags |= BallFlags.MarkedForMatch;
            }

            return affectedBalls;
        }

        /// <summary>
        /// Draw debug visualization for the lightning strike area
        /// </summary>
        public void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            // Get grid manager
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return;

            // Convert impact position to grid position
            Vector2Int gridPos = GridUtils.WorldToPos(impactPosition, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);

            // Calculate world positions for left and right extents
            float ballWidth = gridManager.BallWidth;
            float y = impactPosition.y;

            // Left extent
            Vector2Int leftPos = new Vector2Int(Mathf.Max(0, gridPos.x - _horizontalRange), gridPos.y);
            Vector3 leftWorldPos = GridUtils.PosToWorld(leftPos, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);

            // Right extent
            Vector2Int rightPos = new Vector2Int(gridPos.x + _horizontalRange, gridPos.y);
            Vector3 rightWorldPos = GridUtils.PosToWorld(rightPos, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);

            // Draw horizontal line
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f); // Yellow
            Gizmos.DrawLine(new Vector3(leftWorldPos.x - ballWidth / 2, y, 0), new Vector3(rightWorldPos.x + ballWidth / 2, y, 0));

            // Draw thicker line for emphasis
            float thickness = 0.1f;
            for (int i = -2; i <= 2; i++)
            {
                float offset = i * thickness * 0.2f;
                Gizmos.DrawLine(
                    new Vector3(leftWorldPos.x - ballWidth / 2, y + offset, 0),
                    new Vector3(rightWorldPos.x + ballWidth / 2, y + offset, 0)
                );
            }

            // Draw impact point
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(impactPosition, 0.15f);
        }

        #endregion

        private void OnEnable()
        {
            Debug.Log($"[LightningBall] Lightning ball activated! Horizontal range: {_horizontalRange} balls left and right");
        }

        private void OnDisable()
        {
            // Reset color when disabled
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.white;
            }
            _sparkTimer = 0f;
            _glowTimer = 0f;
        }
    }
}