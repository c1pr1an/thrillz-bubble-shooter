using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    /// <summary>
    /// Manages ball destruction with timing and animations
    /// Destroys balls sequentially with small delays
    /// Adapted from BubbleShooterGameToolkit reference implementation
    /// </summary>
    public class DestroyManager : UnitySingleton<DestroyManager>
    {
        [Header("Settings")]
        [SerializeField] private float delayBetweenDestructions = 0.05f;

        private bool isDestroying = false;

        /// <summary>
        /// Destroys a list of balls with sequential timing
        /// </summary>
        public void DestroyBalls(List<Ball> balls)
        {
            if (balls == null || balls.Count == 0) return;

            // Start destruction coroutine
            StartCoroutine(DestroyBallsSequence(balls));
        }

        /// <summary>
        /// Coroutine to destroy balls one by one with delays
        /// </summary>
        private IEnumerator DestroyBallsSequence(List<Ball> balls)
        {
            isDestroying = true;

            // Sort by distance from center for nice visual effect
            Vector3 center = Vector3.zero;
            if (balls.Count > 0)
            {
                // Calculate center position of all balls
                foreach (Ball ball in balls)
                {
                    if (ball != null)
                    {
                        center += ball.transform.position;
                    }
                }
                center /= balls.Count;
            }

            // Sort by distance from center (closest first)
            balls.Sort((a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;
                float distA = Vector3.Distance(a.transform.position, center);
                float distB = Vector3.Distance(b.transform.position, center);
                return distA.CompareTo(distB);
            });

            // Destroy each ball
            foreach (Ball ball in balls)
            {
                if (ball != null && ball.gameObject != null)
                {
                    // Mark as destroying
                    ball.Flags |= BallFlags.MarkedForDestroy;
                    ball.Flags |= BallFlags.Destroying;

                    // Remove from grid
                    if (GridManager.Exists())
                    {
                        GridManager.Instance.RemoveBall(ball);
                    }

                    // Destroy the ball (triggers animation in Ball.DestroyBall())
                    ball.DestroyBall();

                    // Wait before next destruction
                    yield return new WaitForSeconds(delayBetweenDestructions);
                }
            }

            isDestroying = false;
        }

        /// <summary>
        /// Instantly destroys a single ball without delay
        /// </summary>
        public void DestroyBallInstantly(Ball ball)
        {
            if (ball == null) return;

            // Mark as destroying
            ball.Flags |= BallFlags.MarkedForDestroy;
            ball.Flags |= BallFlags.Destroying;

            // Remove from grid
            if (GridManager.Exists())
            {
                GridManager.Instance.RemoveBall(ball);
            }

            // Destroy immediately
            ball.DestroyBall();
        }

        /// <summary>
        /// Returns true if currently destroying balls
        /// </summary>
        public bool IsDestroying()
        {
            return isDestroying;
        }
    }
}
