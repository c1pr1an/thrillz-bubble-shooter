using System.Collections;
using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class DestroyManager : UnitySingleton<DestroyManager>
    {
        // Serialized Fields
        [Header("Settings")]
        [SerializeField] private float _delayBetweenDestructions = 0.05f;

        // Private Fields
        private bool _isDestroying = false;

        // Public Methods
        public void DestroyBalls(List<Ball> balls)
        {
            if (balls == null || balls.Count == 0) return;

            // Start destruction coroutine
            StartCoroutine(DestroyBallsSequence(balls));
        }

        // Private Methods - Coroutines for destroying balls one by one with delays
        private IEnumerator DestroyBallsSequence(List<Ball> balls)
        {
            _isDestroying = true;

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
                    GridManager.Instance.RemoveBall(ball);

                    // Destroy the ball (triggers animation in Ball.DestroyBall())
                    ball.DestroyBall();

                    // Wait before next destruction
                    yield return new WaitForSeconds(_delayBetweenDestructions);
                }
            }

            _isDestroying = false;
        }

        public void DestroyBallInstantly(Ball ball)
        {
            if (ball == null) return;

            // Mark as destroying
            ball.Flags |= BallFlags.MarkedForDestroy;
            ball.Flags |= BallFlags.Destroying;

            // Remove from grid
            GridManager.Instance.RemoveBall(ball);

            // Destroy immediately
            ball.DestroyBall();
        }

        public bool IsDestroying()
        {
            return _isDestroying;
        }
    }
}
