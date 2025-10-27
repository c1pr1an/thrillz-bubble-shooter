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

            // Special handling for Rainbow balls
            if (stoppedBall.IsRainbow())
            {
                matchCount = CheckRainbowMatch(stoppedBall);
            }
            else
            {
                matchCount = CheckMatch(stoppedBall);
            }

            if (matchCount >= 3)
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

        /// <summary>
        /// Check match for Rainbow ball - matches all adjacent colors
        /// </summary>
        public int CheckRainbowMatch(Ball rainbowBall)
        {
            if (rainbowBall == null) return 0;

            _matchList.Clear();

            // Add the rainbow ball itself
            _matchList.Add(rainbowBall);
            rainbowBall.Flags |= BallFlags.MarkedForMatch;

            // Find all adjacent balls and match ALL colors
            HashSet<BallColor> colorsToMatch = new HashSet<BallColor>();

            // First, identify all colors touching the rainbow ball
            foreach (Ball neighbor in rainbowBall.Neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    colorsToMatch.Add(neighbor.Color);
                }
            }

            // Now find all connected balls for each color
            foreach (BallColor color in colorsToMatch)
            {
                foreach (Ball neighbor in rainbowBall.Neighbors)
                {
                    if (neighbor != null && neighbor.Color == color)
                    {
                        FindMatches(neighbor, color);
                    }
                }
            }

            // Clear marks
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
