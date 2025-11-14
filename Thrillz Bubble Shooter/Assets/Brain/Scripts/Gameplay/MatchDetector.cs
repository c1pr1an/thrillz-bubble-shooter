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
                // Get the bonus ball base class
                BonusBallBase bonusBall = stoppedBall.GetComponent<BonusBallBase>();

                if (bonusBall != null)
                {
                    // Special handling for rocket ball which needs direction
                    Vector2 impactDirection = Vector2.zero;
                    if (stoppedBall.IsRocket())
                    {
                        var rocket = bonusBall as RocketBall;
                        if (rocket != null)
                        {
                            impactDirection = rocket.GetLastVelocity();
                        }
                    }

                    // Use the detector to get affected balls
                    detectedBalls = bonusBall.GetAffectedBalls(stoppedBall.transform.position, impactDirection);
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

            // Handle streak and sound effects based on ball type
            if (stoppedBall.IsBonusBall == false)
            {
                if (shouldDestroy)
                    ScoreManager.Instance.IncreaseStreak();
                else ScoreManager.Instance.ResetStreak();
            }

            // Store matched balls for later destruction
            List<Ball> matchedBalls = shouldDestroy ? new List<Ball>(_matchList) : new List<Ball>();

            int ballScoreValue = ScoreManager.Instance.GetCurrentBallScore();
            int orphanScoreValue = ScoreManager.Instance.GetCurrentOrphanScore();

            // Step 1: Mark matched balls for destruction (but don't destroy them yet)
            if (shouldDestroy)
            {
                foreach (Ball ball in matchedBalls)
                {
                    if (ball != null)
                    {
                        ball.Flags |= BallFlags.MarkedForDestroy;
                    }
                }
            }

            // Step 2: Check for orphaned balls (they will ignore balls marked for destruction)
            OrphanDetector.Instance.CheckSeparatedBalls();

            // Wait for orphan detection to complete
            yield return new WaitWhile(() => OrphanDetector.Instance.IsChecking());

            // Get orphaned balls that were detected
            List<Ball> orphanedBalls = OrphanDetector.Instance.LastOrphanedBalls;

            // Step 3: Combine all balls that will be removed
            List<Ball> allBallsToRemove = new List<Ball>();
            allBallsToRemove.AddRange(matchedBalls);
            allBallsToRemove.AddRange(orphanedBalls);

            // Step 4: First destroy matched balls
            if (shouldDestroy)
            {
                // Pass the impact ball (stoppedBall) and score value to create wave pattern from impact point
                DestroyManager.Instance.DestroyBalls(matchedBalls, stoppedBall, ballScoreValue);

                yield return new WaitWhile(() => DestroyManager.Instance.IsDestroying());

                ScoreManager.Instance.PlayStreakSound();
            }

            // Step 5: Start orphan falling animations (don't wait for them to complete)
            if (orphanedBalls.Count > 0)
            {
                OrphanDetector.Instance.AnimateOrphanFalling(orphanScoreValue);
            }

            yield return new WaitForSeconds(0.1f);

            // Step 6: NOW move the grid after destruction started
            if (allBallsToRemove.Count > 0)
            {
                GridScrollManager.Instance.PreCalculateAndMoveGrid(allBallsToRemove);
            }
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
