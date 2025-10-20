using System.Collections.Generic;
using Brain.Gameplay;
using Brain.Util;
using UnityEngine;

namespace Brain.Managers
{
    public class MatchingManager : UnitySingleton<MatchingManager>
    {
        // Serialized Fields
        [Header("Settings")]
        [SerializeField] private int _minMatchCount = 3;

        // Private Fields
        private List<Ball> _matchList = new List<Ball>();

        // Public Methods - Main entry point when a ball stops on the grid
        public void ProcessBallStopped(Ball stoppedBall)
        {
            if (stoppedBall == null) return;

            int matchCount = CheckMatch(stoppedBall);

            if (matchCount >= _minMatchCount)
            {
                DestroyManager.Instance.DestroyBalls(_matchList);
            }

            SeparatingBallManager.Instance.CheckSeparatedBalls();
            GridScrollManager.Instance.UpdateGridPosition();
            GameConditionsManager.Instance.CheckWinCondition();
        }

        public int CheckMatch(Ball ball)
        {
            if (ball == null) return 0;

            // Clear previous match list
            _matchList.Clear();

            // Start flood-fill from this ball
            FindMatches(ball, ball.Color);

            // Clear marks after checking
            ClearMarks();

            return _matchList.Count;
        }

        // Private Methods - Recursive flood-fill to find all connected balls of same color
        private void FindMatches(Ball ball, BallColor targetColor)
        {
            if (ball == null) return;

            // Skip if already marked or wrong color or not pinned
            if (ball.HasFlag(BallFlags.MarkedForMatch)) return;
            if (ball.Color != targetColor) return;
            if (!ball.HasFlag(BallFlags.Pinned)) return;
            if (ball.HasFlag(BallFlags.Destroying)) return;

            // Mark this ball as checked
            ball.Flags |= BallFlags.MarkedForMatch;
            _matchList.Add(ball);

            // Recursively check all neighbors
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
