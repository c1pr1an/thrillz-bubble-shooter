using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class DestroyManager : UnitySingleton<DestroyManager>
    {
        private float _delayBetweenDestructions = 0.03f;

        // Private Fields
        private bool _isDestroying = false;

        // Public Methods
        public void DestroyBalls(List<Ball> balls, Ball impactBall = null, int scorePerBall = -1)
        {
            if (balls == null || balls.Count == 0) return;

            // Start destruction coroutine
            StartCoroutine(DestroyBallsSequence(balls, impactBall, scorePerBall));
        }

        // Private Methods - Coroutines for destroying balls one by one with delays
        private IEnumerator DestroyBallsSequence(List<Ball> balls, Ball impactBall = null, int scorePerBall = -1)
        {
            _isDestroying = true;

            // Track destroyed balls for power system
            int destroyedCount = 0;

            // Check if the impact ball is a rainbow ball
            bool useRainbowVFX = impactBall != null && impactBall.IsRainbow();

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

            // Use passed score value or get from current streak if not provided
            if (scorePerBall == -1)
            {
                scorePerBall = ScoreManager.Instance.GetCurrentBallScore();
            }

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

                    ball.Flags |= BallFlags.MarkedForDestroy;
                    ball.Flags |= BallFlags.Destroying;
                    GridManager.Instance.RemoveBall(ball);
                    ball.DestroyBall(scorePerBall, useRainbowVFX);
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

        public void DestroyBallInstantly(Ball ball, int scoreValue = -1, bool useRainbowVFX = false)
        {
            if (ball == null) return;

            // Mark as destroying
            ball.Flags |= BallFlags.MarkedForDestroy;
            ball.Flags |= BallFlags.Destroying;

            // Remove from grid
            GridManager.Instance.RemoveBall(ball);

            // Use current streak score if not specified
            if (scoreValue == -1)
            {
                scoreValue = ScoreManager.Instance.GetCurrentBallScore();
            }

            // Destroy immediately
            ball.DestroyBall(scoreValue, useRainbowVFX);
        }

        public bool IsDestroying()
        {
            return _isDestroying;
        }
    }
}
