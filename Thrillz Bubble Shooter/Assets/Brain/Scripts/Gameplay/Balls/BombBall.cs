using System.Collections.Generic;
using UnityEngine;
using Brain.Managers;
using Brain.Util;
using Brain.Audio;

namespace Brain.Gameplay
{
    /// <summary>
    /// Component that marks a ball as a Bomb bonus ball.
    /// Bomb balls destroy all balls within 2 grid positions on impact.
    /// </summary>
    [RequireComponent(typeof(Ball))]
    public class BombBall : BonusBallBase
    {
        private Ball _ball;

        [Header("Bomb Settings")]
        [SerializeField] private int _explosionRadius = 2; // Grid positions

        private void Awake()
        {
            _ball = GetComponent<Ball>();
        }

        public override void ActivateIdleParticles()
        {
            base.ActivateIdleParticles();
            SoundManager.Instance.PlaySfxLoop(SoundType.Game_BoosterWick_Loop);
        }

        public bool IsBomb()
        {
            return enabled && _ball != null;
        }

        public int GetExplosionRadius()
        {
            return _explosionRadius;
        }

        #region BonusBall Implementation

        /// <summary>
        /// Get all balls that would be affected by the bomb explosion
        /// </summary>
        public override List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            List<Ball> affectedBalls = new List<Ball>();

            // Get grid manager
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null || _ball == null)
                return affectedBalls;

            // Get the bomb's grid position
            // If impact position is provided and different from ball position, use that (for preview)
            Vector2Int bombGridPos;
            if (impactPosition != Vector2.zero && (Vector2)_ball.transform.position != impactPosition)
            {
                // Convert impact position to grid position for preview
                bombGridPos = GridUtils.WorldToPos(impactPosition, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);
            }
            else
            {
                // Use ball's actual grid position (for actual detection after landing)
                bombGridPos = _ball.GridPosition;
            }

            // Get all positions within explosion radius using GridUtils method
            List<Vector2Int> explosionPositions = GridUtils.GetExtendedNeighborPositions(bombGridPos, _explosionRadius);

            // Add all balls at these positions to the affected list
            foreach (Vector2Int pos in explosionPositions)
            {
                Ball ballAtPos = gridManager.GetBall(pos.x, pos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    affectedBalls.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Add the bomb ball itself to be destroyed
            if (!affectedBalls.Contains(_ball))
            {
                affectedBalls.Add(_ball);
                _ball.Flags |= BallFlags.MarkedForMatch;
            }

            return affectedBalls;
        }

        /// <summary>
        /// Draw debug visualization for the explosion radius
        /// </summary>
        public override void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default)
        {
            // Get grid manager
            GridManager gridManager = GridManager.Instance;
            if (gridManager == null)
                return;

            // Convert impact position to grid position
            Vector2Int gridPos = GridUtils.WorldToPos(impactPosition, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);

            // Get all positions within explosion radius
            List<Vector2Int> explosionPositions = GridUtils.GetExtendedNeighborPositions(gridPos, _explosionRadius);

            // Draw circles for each affected position
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // Red with transparency
            foreach (Vector2Int pos in explosionPositions)
            {
                Vector3 worldPos = GridUtils.PosToWorld(pos, gridManager.BallWidth, gridManager.BallHeight, gridManager.GridContainer);
                Gizmos.DrawWireSphere(worldPos, gridManager.BallWidth * 0.4f);
            }

            // Draw explosion center
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(impactPosition, 0.2f);

            // Draw explosion radius circle
            float explosionWorldRadius = _explosionRadius * gridManager.BallHeight;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
            DrawCircle(impactPosition, explosionWorldRadius, 32);
        }

        private void DrawCircle(Vector2 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector2(radius, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }

        #endregion

        private void OnEnable()
        {
            Debug.Log($"[BombBall] Bomb ball activated! Explosion radius: {_explosionRadius} grid positions");
        }
    }
}