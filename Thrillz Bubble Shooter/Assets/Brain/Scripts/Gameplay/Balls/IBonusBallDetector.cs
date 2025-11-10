using System.Collections.Generic;
using UnityEngine;

namespace Brain.Gameplay
{
    /// <summary>
    /// Interface for bonus balls that have special detection patterns
    /// for determining which balls they affect when they land.
    /// </summary>
    public interface IBonusBall
    {
        /// <summary>
        /// Get all balls that would be affected by this bonus ball's special effect.
        /// </summary>
        /// <param name="impactPosition">The position where the ball impacts/lands</param>
        /// <param name="impactDirection">The direction of impact (mainly used for directional effects like rocket)</param>
        /// <returns>List of balls that will be destroyed/affected</returns>
        List<Ball> GetAffectedBalls(Vector2 impactPosition, Vector2 impactDirection = default);

        /// <summary>
        /// Draw debug visualization in the editor to show the detection area.
        /// Called from OnDrawGizmos.
        /// </summary>
        /// <param name="impactPosition">The position where the ball would impact</param>
        /// <param name="impactDirection">The direction of impact (if applicable)</param>
        void DrawDebugVisualization(Vector2 impactPosition, Vector2 impactDirection = default);

        /// <summary>
        /// Set whether the bonus ball is in the launcher container.
        /// Used to activate/deactivate idle particles and other launcher-specific behavior.
        /// </summary>
        /// <param name="inLauncher">True if the ball is in the launcher, false otherwise</param>
        void SetInLauncher(bool inLauncher);
    }
}