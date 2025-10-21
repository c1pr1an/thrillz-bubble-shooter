using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brain.Managers;
using Brain.Util;
using UnityEngine;

namespace Brain.Gameplay
{
    public class OrphanDetector : UnitySingleton<OrphanDetector>
    {
        [Header("Settings")]
        [SerializeField] private int _maxIterations = 5;
        [SerializeField] private float _delayBetweenFalls = 0.05f;

        private HashSet<Ball> _connectedBalls = new HashSet<Ball>();
        private bool _isChecking = false;

        public bool IsChecking() => _isChecking;

        public void CheckSeparatedBalls()
        {
            StartCoroutine(CheckSeparatedBallsCoroutine());
        }

        private IEnumerator CheckSeparatedBallsCoroutine()
        {
            _isChecking = true;

            for (int iteration = 0; iteration < _maxIterations; iteration++)
            {
                List<Ball> ballsToFall = FindOrphanedBalls();

                if (ballsToFall.Count == 0)
                {
                    break;
                }

                ballsToFall = ballsToFall.OrderByDescending(ball => ball.Position.y).ToList();

                foreach (Ball ball in ballsToFall)
                {
                    if (ball != null)
                    {
                        GridManager.Instance.RemoveBall(ball);

                        ball.Fall();

                        yield return new WaitForSeconds(_delayBetweenFalls);
                    }
                }

                yield return new WaitForSeconds(0.2f);
            }

            _isChecking = false;
        }

        private List<Ball> FindOrphanedBalls()
        {
            GridManager gridManager = GridManager.Instance;

            _connectedBalls.Clear();

            gridManager.ClearAllMarks();

            foreach (Ball rootBall in Ball.s_rootBalls)
            {
                if (rootBall != null && rootBall.HasFlag(BallFlags.Pinned) && !rootBall.HasFlag(BallFlags.Destroying))
                {
                    FindConnectedBalls(rootBall);
                }
            }

            List<Ball> orphanedBalls = new List<Ball>();
            var balls = gridManager.Balls;

            for (int row = 0; row < balls.Count; row++)
            {
                for (int col = 0; col < balls[row].Count; col++)
                {
                    Ball ball = balls[row][col];

                    if (ball != null &&
                        ball.HasFlag(BallFlags.Pinned) &&
                        !ball.HasFlag(BallFlags.MarkedForDestroy) &&
                        !ball.HasFlag(BallFlags.Destroying) &&
                        !_connectedBalls.Contains(ball))
                    {
                        orphanedBalls.Add(ball);
                    }
                }
            }

            return orphanedBalls;
        }

        private void FindConnectedBalls(Ball ball)
        {
            if (ball == null) return;
            if (_connectedBalls.Contains(ball)) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;
            if (ball.HasFlag(BallFlags.MarkConnected)) return;

            ball.Flags |= BallFlags.MarkConnected;
            _connectedBalls.Add(ball);

            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    FindConnectedBalls(neighbor);
                }
            }
        }
    }
}
