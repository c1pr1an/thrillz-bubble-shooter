using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class DestroyManager : UnitySingleton<DestroyManager>
    {
        private float _delayBetweenDestructions = 0.02f;

        // Private Fields
        private bool _isDestroying = false;

        // Public Methods
        public void DestroyBalls(List<Ball> balls, Ball impactBall = null)
        {
            if (balls == null || balls.Count == 0) return;

            // Start destruction coroutine
            StartCoroutine(DestroyBallsSequence(balls, impactBall));
        }

        // Private Methods - Coroutines for destroying balls one by one with delays
        private IEnumerator DestroyBallsSequence(List<Ball> balls, Ball impactBall = null)
        {
            _isDestroying = true;

            // Track destroyed balls for power system
            int destroyedCount = 0;

            // Sort balls by distance from impact point (wave pattern)
            if (impactBall != null && balls.Contains(impactBall))
            {
                Vector3 impactPoint = impactBall.transform.position;

                // Sort by distance from impact point (closest first)
                balls.Sort((a, b) =>
                {
                    if (a == null) return 1;
                    if (b == null) return -1;
                    float distA = Vector3.Distance(a.transform.position, impactPoint);
                    float distB = Vector3.Distance(b.transform.position, impactPoint);
                    return distA.CompareTo(distB);
                });
            }
            else if (balls.Count > 0 && balls[0] != null)
            {
                // Fallback: use first ball as impact point
                Vector3 impactPoint = balls[0].transform.position;

                balls.Sort((a, b) =>
                {
                    if (a == null) return 1;
                    if (b == null) return -1;
                    float distA = Vector3.Distance(a.transform.position, impactPoint);
                    float distB = Vector3.Distance(b.transform.position, impactPoint);
                    return distA.CompareTo(distB);
                });
            }

            // Track ball index for progressive scoring
            int ballIndex = 0;

            // Destroy each ball
            foreach (Ball ball in balls)
            {
                if (ball != null && ball.gameObject != null)
                {
                    // Don't count rainbow balls themselves toward power
                    if (!ball.IsRainbow())
                    {
                        destroyedCount++;
                    }

                    // Calculate progressive score
                    int scoreValue = CalculateProgressiveScore(ballIndex);
                    ballIndex++;

                    ball.Flags |= BallFlags.MarkedForDestroy;
                    ball.Flags |= BallFlags.Destroying;
                    GridManager.Instance.RemoveBall(ball);
                    ball.DestroyBall(scoreValue);
                    yield return new WaitForSeconds(_delayBetweenDestructions);
                }
            }

            // Add power for destroyed balls
            if (destroyedCount > 0)
            {
                BonusPowerManager.Instance.AddPower(destroyedCount);
            }

            _isDestroying = false;
        }

        /// <summary>
        /// Calculate progressive score based on ball position in destruction sequence
        /// First 3 balls: 10 points each
        /// Then increases by +10 per ball up to max 100
        /// </summary>
        private int CalculateProgressiveScore(int ballIndex)
        {
            if (ballIndex < 3)
            {
                // First 3 balls get 10 points each
                return 10;
            }
            else
            {
                // Starting from 4th ball: 20, 30, 40... up to 100
                int progressiveScore = (ballIndex - 2) * 10 + 10;
                return Mathf.Min(progressiveScore, 100); // Cap at 100
            }
        }

        public void DestroyBallInstantly(Ball ball, int scoreValue = 10)
        {
            if (ball == null) return;

            // Mark as destroying
            ball.Flags |= BallFlags.MarkedForDestroy;
            ball.Flags |= BallFlags.Destroying;

            // Remove from grid
            GridManager.Instance.RemoveBall(ball);

            // Destroy immediately
            ball.DestroyBall(scoreValue);
        }

        public bool IsDestroying()
        {
            return _isDestroying;
        }
    }
}
