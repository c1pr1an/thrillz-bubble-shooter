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

            // Special handling for Rocket balls
            if (stoppedBall.IsRocket())
            {
                matchCount = CheckRocketPath(stoppedBall);
            }
            // Special handling for Lightning balls
            else if (stoppedBall.IsLightning())
            {
                matchCount = CheckLightningStrike(stoppedBall);
            }
            // Special handling for Bomb balls
            else if (stoppedBall.IsBomb())
            {
                matchCount = CheckBombExplosion(stoppedBall);
            }
            // Special handling for Rainbow balls
            else if (stoppedBall.IsRainbow())
            {
                matchCount = CheckRainbowMatch(stoppedBall);
            }
            else
            {
                matchCount = CheckMatch(stoppedBall);
            }

            if (matchCount >= 3 || (stoppedBall.IsBomb() && matchCount > 0) || (stoppedBall.IsLightning() && matchCount > 0) || (stoppedBall.IsRocket() && matchCount > 0))
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
        /// Check rocket path - destroys balls in the shooting direction
        /// </summary>
        public int CheckRocketPath(Ball rocketBall)
        {
            if (rocketBall == null) return 0;

            _matchList.Clear();

            // Get rocket component to check settings and direction
            var rocketComponent = rocketBall.GetComponent<RocketBall>();
            if (rocketComponent == null) return 0;

            int ballsPerRow = rocketComponent.GetBallsPerRow();
            int maxRows = rocketComponent.GetMaxRows();
            Vector2 impactDirection = rocketComponent.GetLastVelocity();

            // If no direction stored, can't determine path
            if (impactDirection == Vector2.zero)
            {
                Debug.LogWarning("[MatchDetector] Rocket ball has no impact direction!");
                return 0;
            }

            // Get the rocket's grid position
            Vector2Int rocketPos = rocketBall.GridPosition;

            // Normalize direction and determine primary axis
            impactDirection = impactDirection.normalized;

            // The rocket continues forward after landing, clearing balls in its path
            // Search in the same direction as the impact (forward from landing point)
            Vector2 searchDirection = impactDirection; // Continue in shooting direction

            // For each row distance
            for (int row = 1; row <= maxRows; row++)
            {
                List<Ball> ballsInRow = new List<Ball>();

                // Calculate approximate row position in the search direction
                float searchDistance = row * GridManager.Instance.BallHeight;
                Vector2 rowCenter = (Vector2)rocketBall.transform.position + searchDirection * searchDistance;

                // Find balls near this row position
                // We'll check a wider area and filter by distance to the direction line
                float rowTolerance = GridManager.Instance.BallHeight * 0.6f;

                // Check all balls in the grid
                for (int y = 0; y < GridManager.Instance.MaxRows; y++)
                {
                    for (int x = 0; x < GridUtils.GetMaxColumns(y); x++)
                    {
                        Ball ball = GridManager.Instance.GetBall(x, y);
                        if (ball != null && ball != rocketBall && ball.HasFlag(BallFlags.Pinned) && !ball.HasFlag(BallFlags.Destroying))
                        {
                            // Check if this ball is roughly at the right distance for this row
                            float distanceFromRocket = Vector2.Distance(ball.transform.position, rocketBall.transform.position);
                            float expectedDistance = row * GridManager.Instance.BallHeight;

                            if (Mathf.Abs(distanceFromRocket - expectedDistance) <= rowTolerance)
                            {
                                // Check if the ball is close to the direction line
                                Vector2 toBall = (Vector2)(ball.transform.position - rocketBall.transform.position);
                                float dotProduct = Vector2.Dot(toBall.normalized, searchDirection);

                                // Ball should be in the search direction (dot > 0.5 means roughly in direction)
                                if (dotProduct > 0.5f)
                                {
                                    // Calculate perpendicular distance from the direction line
                                    Vector2 projection = Vector2.Dot(toBall, searchDirection) * searchDirection;
                                    Vector2 perpendicular = toBall - projection;
                                    float perpendicularDistance = perpendicular.magnitude;

                                    // Consider balls within a reasonable perpendicular distance
                                    if (perpendicularDistance <= GridManager.Instance.BallWidth * 1.5f)
                                    {
                                        ballsInRow.Add(ball);
                                    }
                                }
                            }
                        }
                    }
                }

                // Sort balls by distance from the direction line (closest first)
                ballsInRow.Sort((a, b) =>
                {
                    Vector2 toA = (Vector2)(a.transform.position - rocketBall.transform.position);
                    Vector2 projA = Vector2.Dot(toA, searchDirection) * searchDirection;
                    float distA = (toA - projA).magnitude;

                    Vector2 toB = (Vector2)(b.transform.position - rocketBall.transform.position);
                    Vector2 projB = Vector2.Dot(toB, searchDirection) * searchDirection;
                    float distB = (toB - projB).magnitude;

                    return distA.CompareTo(distB);
                });

                // Take up to ballsPerRow balls from this row
                int ballsToTake = Mathf.Min(ballsInRow.Count, ballsPerRow);
                for (int i = 0; i < ballsToTake; i++)
                {
                    _matchList.Add(ballsInRow[i]);
                    ballsInRow[i].Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Add the rocket ball itself to be destroyed
            if (!_matchList.Contains(rocketBall))
            {
                _matchList.Add(rocketBall);
                rocketBall.Flags |= BallFlags.MarkedForMatch;
            }

            Debug.Log($"[MatchDetector] Rocket at {rocketPos} destroyed {_matchList.Count} balls continuing in direction {searchDirection}");

            return _matchList.Count;
        }

        /// <summary>
        /// Check lightning strike - destroys balls horizontally
        /// </summary>
        public int CheckLightningStrike(Ball lightningBall)
        {
            if (lightningBall == null) return 0;

            _matchList.Clear();

            // Get lightning component to check range
            var lightningComponent = lightningBall.GetComponent<LightningBall>();
            int horizontalRange = lightningComponent != null ? lightningComponent.GetHorizontalRange() : 4;

            // Get the lightning ball's grid position
            Vector2Int lightningPos = lightningBall.GridPosition;

            // Collect balls to the left
            for (int x = lightningPos.x - 1; x >= lightningPos.x - horizontalRange; x--)
            {
                if (!GridUtils.IsValidPosition(x, lightningPos.y))
                    break;

                Ball ballAtPos = GridManager.Instance.GetBall(x, lightningPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    _matchList.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Collect balls to the right
            for (int x = lightningPos.x + 1; x <= lightningPos.x + horizontalRange; x++)
            {
                if (!GridUtils.IsValidPosition(x, lightningPos.y))
                    break;

                Ball ballAtPos = GridManager.Instance.GetBall(x, lightningPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    _matchList.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Add the lightning ball itself to be destroyed
            if (!_matchList.Contains(lightningBall))
            {
                _matchList.Add(lightningBall);
                lightningBall.Flags |= BallFlags.MarkedForMatch;
            }

            Debug.Log($"[MatchDetector] Lightning strike at {lightningPos} destroyed {_matchList.Count} balls");

            return _matchList.Count;
        }

        /// <summary>
        /// Check explosion for Bomb ball - destroys all balls within radius
        /// </summary>
        public int CheckBombExplosion(Ball bombBall)
        {
            if (bombBall == null) return 0;

            _matchList.Clear();

            // Get bomb component to check radius
            var bombComponent = bombBall.GetComponent<BombBall>();
            int explosionRadius = bombComponent != null ? bombComponent.GetExplosionRadius() : 2;

            Vector2Int bombGridPos = bombBall.GridPosition;

            // Get all positions within explosion radius using our new GridUtils method
            List<Vector2Int> explosionPositions = GridUtils.GetExtendedNeighborPositions(bombGridPos, explosionRadius);

            // Add all balls at these positions to the match list
            foreach (Vector2Int pos in explosionPositions)
            {
                Ball ballAtPos = GridManager.Instance.GetBall(pos.x, pos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    _matchList.Add(ballAtPos);
                    ballAtPos.Flags |= BallFlags.MarkedForMatch;
                }
            }

            // Add the bomb ball itself to be destroyed
            if (!_matchList.Contains(bombBall))
            {
                _matchList.Add(bombBall);
                bombBall.Flags |= BallFlags.MarkedForMatch;
            }

            Debug.Log($"[MatchDetector] Bomb explosion at {bombGridPos} destroyed {_matchList.Count} balls");

            return _matchList.Count;
        }

        /// <summary>
        /// Check match for Rainbow ball - only matches color groups of 3+ (including rainbow)
        /// </summary>
        public int CheckRainbowMatch(Ball rainbowBall)
        {
            if (rainbowBall == null) return 0;

            _matchList.Clear();
            List<Ball> finalMatchList = new List<Ball>();
            HashSet<Ball> processedBalls = new HashSet<Ball>();

            // Track which colors to check
            HashSet<BallColor> colorsToCheck = new HashSet<BallColor>();

            // First, identify all colors directly touching the rainbow ball
            foreach (Ball neighbor in rainbowBall.Neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    colorsToCheck.Add(neighbor.Color);
                }
            }

            // For each color, check if we have 3+ balls (including the rainbow)
            foreach (BallColor color in colorsToCheck)
            {
                // Clear the match list for this color check
                _matchList.Clear();

                // Find all neighbors of this color and flood fill from one
                Ball startBall = null;
                foreach (Ball neighbor in rainbowBall.Neighbors)
                {
                    if (neighbor != null && neighbor.Color == color && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        startBall = neighbor;
                        break;
                    }
                }

                if (startBall == null) continue;

                // Find all connected balls of this color
                FindMatches(startBall, color);

                // Clear marks for this color group
                foreach (Ball ball in _matchList)
                {
                    if (ball != null)
                    {
                        ball.Flags &= ~BallFlags.MarkedForMatch;
                    }
                }

                // If this color group + rainbow ball makes 3 or more, include them
                if (_matchList.Count >= 2) // 2 color balls + 1 rainbow = 3 total
                {
                    foreach (Ball ball in _matchList)
                    {
                        if (!processedBalls.Contains(ball))
                        {
                            finalMatchList.Add(ball);
                            processedBalls.Add(ball);
                        }
                    }
                }
            }

            // Only add rainbow ball if we found valid matches
            if (finalMatchList.Count > 0)
            {
                finalMatchList.Add(rainbowBall);
                rainbowBall.Flags |= BallFlags.MarkedForMatch;
            }

            // Set the final match list
            _matchList = finalMatchList;

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

        /// <summary>
        /// Get preview of what would be matched if a ball landed at a position
        /// Simulates both regular and bonus ball behavior
        /// </summary>
        public List<Ball> GetMatchPreviewAtPosition(Ball simulatedBall, Ball targetPosition)
        {
            if (targetPosition == null || simulatedBall == null) return new List<Ball>();

            // Check if it's a rocket ball
            if (simulatedBall.IsRocket())
            {
                return GetRocketPathPreview(simulatedBall, targetPosition);
            }
            // Check if it's a lightning ball
            else if (simulatedBall.IsLightning())
            {
                return GetLightningStrikePreview(simulatedBall, targetPosition);
            }
            // Check if it's a bomb ball
            else if (simulatedBall.IsBomb())
            {
                return GetBombExplosionPreview(simulatedBall, targetPosition);
            }
            // Check if it's a rainbow ball
            else if (simulatedBall.IsRainbow())
            {
                return GetRainbowMatchPreview(targetPosition);
            }
            else
            {
                // For regular balls, check what would match at that position
                _matchList.Clear();

                // Check if new ball would connect with same-color neighbors
                List<Ball> sameColorNeighbors = new List<Ball>();
                foreach (Ball neighbor in targetPosition.Neighbors)
                {
                    if (neighbor != null && neighbor.Color == simulatedBall.Color &&
                        neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        sameColorNeighbors.Add(neighbor);
                    }
                }

                // If no same-color neighbors, no match possible
                if (sameColorNeighbors.Count == 0)
                    return new List<Ball>();

                // Find all connected balls of the same color
                foreach (Ball neighbor in sameColorNeighbors)
                {
                    FindMatches(neighbor, simulatedBall.Color);
                }

                ClearMarks();

                // Only return if we'd have 3+ matches (including the new ball)
                if (_matchList.Count >= 2) // 2 existing + 1 new = 3
                {
                    // Don't include targetPosition - it's where the new ball would go, not a ball to destroy
                    return new List<Ball>(_matchList);
                }
                return new List<Ball>();
            }
        }

        /// <summary>
        /// Preview what a rocket ball would destroy at a given position
        /// NOTE: This is approximate since we don't have the actual trajectory direction yet
        /// </summary>
        private List<Ball> GetRocketPathPreview(Ball rocketBall, Ball targetPosition)
        {
            // For preview, we can't accurately predict the rocket path since we don't know
            // the trajectory direction yet. Return empty for now - the actual highlighting
            // will be done in BallHighlightManager using the current trajectory.
            return new List<Ball>();
        }

        /// <summary>
        /// Preview what a lightning ball would destroy at a given position
        /// </summary>
        private List<Ball> GetLightningStrikePreview(Ball lightningBall, Ball targetPosition)
        {
            if (targetPosition == null || lightningBall == null) return new List<Ball>();

            List<Ball> previewList = new List<Ball>();

            // Get lightning component to check range
            var lightningComponent = lightningBall.GetComponent<LightningBall>();
            int horizontalRange = lightningComponent != null ? lightningComponent.GetHorizontalRange() : 4;

            // Get the target grid position
            Vector2Int targetPos = targetPosition.GridPosition;

            // Collect balls to the left
            for (int x = targetPos.x - 1; x >= targetPos.x - horizontalRange; x--)
            {
                if (!GridUtils.IsValidPosition(x, targetPos.y))
                    break;

                Ball ballAtPos = GridManager.Instance.GetBall(x, targetPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    previewList.Add(ballAtPos);
                }
            }

            // Collect balls to the right
            for (int x = targetPos.x + 1; x <= targetPos.x + horizontalRange; x++)
            {
                if (!GridUtils.IsValidPosition(x, targetPos.y))
                    break;

                Ball ballAtPos = GridManager.Instance.GetBall(x, targetPos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    previewList.Add(ballAtPos);
                }
            }

            // Don't add the target position - it's just where the lightning would land

            return previewList;
        }

        /// <summary>
        /// Preview what a bomb ball would destroy at a given position
        /// </summary>
        private List<Ball> GetBombExplosionPreview(Ball bombBall, Ball targetPosition)
        {
            if (targetPosition == null || bombBall == null) return new List<Ball>();

            List<Ball> previewList = new List<Ball>();

            // Get bomb component to check radius
            var bombComponent = bombBall.GetComponent<BombBall>();
            int explosionRadius = bombComponent != null ? bombComponent.GetExplosionRadius() : 2;

            // Get the target grid position
            Vector2Int targetGridPos = targetPosition.GridPosition;

            // Get all positions within explosion radius
            List<Vector2Int> explosionPositions = GridUtils.GetExtendedNeighborPositions(targetGridPos, explosionRadius);

            // Add all balls at these positions to preview
            foreach (Vector2Int pos in explosionPositions)
            {
                Ball ballAtPos = GridManager.Instance.GetBall(pos.x, pos.y);
                if (ballAtPos != null && ballAtPos.HasFlag(BallFlags.Pinned) && !ballAtPos.HasFlag(BallFlags.Destroying))
                {
                    previewList.Add(ballAtPos);
                }
            }

            // Don't add the target position - it's just where the bomb would land
            // The target ball itself doesn't get destroyed since it's an empty position

            return previewList;
        }

        /// <summary>
        /// Preview what a rainbow ball would match at a given position
        /// </summary>
        private List<Ball> GetRainbowMatchPreview(Ball targetPosition)
        {
            if (targetPosition == null) return new List<Ball>();

            List<Ball> previewList = new List<Ball>();
            HashSet<Ball> processedBalls = new HashSet<Ball>();

            // Track which colors to check
            HashSet<BallColor> colorsToCheck = new HashSet<BallColor>();

            // First, identify all colors directly touching the target position
            foreach (Ball neighbor in targetPosition.Neighbors)
            {
                if (neighbor != null && neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                {
                    colorsToCheck.Add(neighbor.Color);
                }
            }

            // For each color, check if we have 3+ balls (including the rainbow)
            foreach (BallColor color in colorsToCheck)
            {
                // Clear the match list for this color check
                _matchList.Clear();

                // Find ONE neighbor of this color to start flood fill
                Ball startBall = null;
                foreach (Ball neighbor in targetPosition.Neighbors)
                {
                    if (neighbor != null && neighbor.Color == color &&
                        neighbor.HasFlag(BallFlags.Pinned) && !neighbor.HasFlag(BallFlags.Destroying))
                    {
                        startBall = neighbor;
                        break;
                    }
                }

                if (startBall == null) continue;

                // Find all connected balls of this color
                FindMatches(startBall, color);

                // Clear marks for this color group
                foreach (Ball ball in _matchList)
                {
                    if (ball != null)
                    {
                        ball.Flags &= ~BallFlags.MarkedForMatch;
                    }
                }

                // If this color group + rainbow ball makes 3 or more, include them
                if (_matchList.Count >= 2) // 2 color balls + 1 rainbow = 3 total
                {
                    foreach (Ball ball in _matchList)
                    {
                        if (!processedBalls.Contains(ball))
                        {
                            previewList.Add(ball);
                            processedBalls.Add(ball);
                        }
                    }
                }
            }

            // Don't add the target position - it's just where the rainbow ball would land
            // The target ball itself doesn't get destroyed

            return previewList;
        }
    }
}
