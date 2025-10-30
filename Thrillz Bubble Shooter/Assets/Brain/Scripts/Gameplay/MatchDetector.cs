using System.Collections;
using System.Collections.Generic;
using Brain.Managers;
using Brain.Util;
using UnityEngine;

namespace Brain.Gameplay
{
    public class MatchDetector : UnitySingleton<MatchDetector>
    {
        private List<Ball> _matchList = new List<Ball>();

        public void ProcessBallStopped(Ball stoppedBall)
        {
            if (stoppedBall == null) return;

            StartCoroutine(ProcessBallStoppedCoroutine(stoppedBall));
        }

        private IEnumerator ProcessBallStoppedCoroutine(Ball stoppedBall)
        {
            int matchCount = 0;

            // Check if this is a bonus ball with custom detection logic
            bool isBonusBall = stoppedBall.IsBonusBall;
            List<Ball> detectedBalls = null;

            if (isBonusBall)
            {
                // Get the bonus ball detector interface
                IBonusBallDetector detector = stoppedBall.GetComponent<IBonusBallDetector>();

                if (detector != null)
                {
                    // Special handling for rocket ball which needs direction
                    Vector2 impactDirection = Vector2.zero;
                    if (stoppedBall.IsRocket())
                    {
                        var rocket = detector as RocketBall;
                        if (rocket != null)
                        {
                            impactDirection = rocket.GetLastVelocity();
                        }
                    }

                    // Use the detector to get affected balls
                    detectedBalls = detector.GetAffectedBalls(stoppedBall.transform.position, impactDirection);
                }
            }

            if (isBonusBall && detectedBalls != null)
            {
                // Use the detected balls
                _matchList = detectedBalls;
                matchCount = _matchList.Count;
            }
            else
            {
                // Regular color matching for non-bonus balls
                matchCount = CheckMatch(stoppedBall);
            }

            // Check if we have enough matches to destroy (3+ for regular, 1+ for bonus balls)
            bool shouldDestroy = matchCount >= 3 || (stoppedBall.IsBonusBall && matchCount > 0);

            if (shouldDestroy)
            {
                // Pass the impact ball (stoppedBall) to create wave pattern from impact point
                DestroyManager.Instance.DestroyBalls(_matchList, stoppedBall);

                yield return new WaitWhile(() => DestroyManager.Instance.IsDestroying());
            }

            // Check for orphaned balls
            OrphanDetector.Instance.CheckSeparatedBalls();

            // Wait only for logic detection to complete, not animations
            yield return new WaitWhile(() => OrphanDetector.Instance.IsChecking());

            // Update grid position immediately after logic detection
            // This happens while balls are still animating their fall
            GridScrollManager.Instance.UpdateGridPosition();
            GameConditionsManager.Instance.CheckWinCondition();
        }

        public int CheckMatch(Ball ball)
        {
            if (ball == null) return 0;

            _matchList.Clear();

            FindMatches(ball, ball.Color);

            ClearMarks();

            return _matchList.Count;
        }

        private void FindMatches(Ball ball, BallColor targetColor)
        {
            if (ball == null) return;

            if (ball.HasFlag(BallFlags.MarkedForMatch)) return;
            if (ball.Color != targetColor) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;

            ball.Flags |= BallFlags.MarkedForMatch;
            _matchList.Add(ball);

            foreach (Ball neighbor in ball.Neighbors)
            {
                if (neighbor != null)
                {
                    FindMatches(neighbor, targetColor);
                }
            }
        }

        private void ClearMarks()
        {
            foreach (Ball ball in _matchList)
            {
                if (ball != null)
                {
                    ball.Flags &= ~BallFlags.MarkedForMatch;
                }
            }
        }

        public List<Ball> GetMatchListPreview(Ball ball)
        {
            if (ball == null) return new List<Ball>();

            _matchList.Clear();
            FindMatches(ball, ball.Color);
            ClearMarks();

            return new List<Ball>(_matchList);
        }
    }
}
