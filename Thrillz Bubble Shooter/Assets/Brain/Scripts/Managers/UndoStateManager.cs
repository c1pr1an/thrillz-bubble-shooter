using System;
using System.Collections.Generic;
using UnityEngine;
using Brain.Gameplay;
using DG.Tweening;
using System.Collections;

namespace Brain.Managers
{
    /// <summary>
    /// Manages the undo system for BlackJack game moves
    /// </summary>
    public class UndoStateManager : MonoBehaviour
    {
        [Header("References")]

        // State storage
        private GameState? lastGameState = null;
        private GameState? LastGameState
        {
            get { return lastGameState; }
            set
            {
                lastGameState = value;
                OnGameStateChanged?.Invoke(value);
            }
        }
        public Action<GameState?> OnGameStateChanged;

        /// <summary>
        /// Initialize the undo manager with required references
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Saves the current game state before a move is made
        /// </summary>
        public void SaveCurrentGameState()
        {
            GameState state = new GameState
            {
                //livesAmount = GameManager.Instance.LivesAmount,
                scoreAmount = ScoreManager.Instance.ScoreCount
            };

            LastGameState = state;
        }

        /// <summary>
        /// Undoes the last move made by the player with smooth animations
        /// </summary>
        public void ExecuteUndo()
        {
            if (!LastGameState.HasValue) return; // No move to undo

            GameState state = LastGameState.Value;

            //GameManager.Instance.LivesAmount = state.livesAmount;
            ScoreManager.Instance.ProcessScoreUndo(state.scoreAmount);


            // Clear the saved state since we've used it
            ClearUndoState();

        }

        /// <summary>
        /// Clears the undo state (called when undo is no longer possible)
        /// </summary>
        public void ClearUndoState()
        {
            LastGameState = null;
        }

        /// <summary>
        /// Checks if undo is available
        /// </summary>
        public bool CanUndo()
        {
            return LastGameState.HasValue;
        }
    }

    [System.Serializable]
    public struct GameState
    {
        public int livesAmount;
        public int scoreAmount;
    }
}
