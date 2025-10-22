using System.Collections;
using System.Collections.Generic;
using Brain.Managers;
using Brain.Util;
using UnityEngine;

namespace Brain.Gameplay
{
    public class MatchDetector : UnitySingleton<MatchDetector>
    {
        [Header("Settings")]
        [SerializeField] private int _minMatchCount = 3;

        private List<Ball> _matchList = new List<Ball>();

        public void ProcessBallStopped(Ball stoppedBall)
        {
            if (stoppedBall == null) return;

            StartCoroutine(ProcessBallStoppedCoroutine(stoppedBall));
        }

        private IEnumerator ProcessBallStoppedCoroutine(Ball stoppedBall)
        {
            int matchCount = CheckMatch(stoppedBall);

            if (matchCount >= _minMatchCount)
            {
                DestroyManager.Instance.DestroyBalls(_matchList);

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
