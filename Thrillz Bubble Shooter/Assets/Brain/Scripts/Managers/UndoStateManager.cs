using System;
using System.Collections.Generic;
using UnityEngine;
using Brain.Gameplay;
using DG.Tweening;
using System.Collections;

namespace Brain.Managers
{
    public class UndoStateManager : MonoBehaviour
    {
        // Private Fields
        private GameState? _lastGameState = null;

        // Properties
        private GameState? LastGameState
        {
            get { return _lastGameState; }
            set
            {
                _lastGameState = value;
                OnGameStateChanged?.Invoke(value);
            }
        }

        // Events
        public Action<GameState?> OnGameStateChanged;

        // Public Methods
        public void Initialize()
        {
        }

        public void SaveCurrentGameState()
        {
            GameState state = new GameState
            {
                //livesAmount = GameManager.Instance.LivesAmount,
                scoreAmount = ScoreManager.Instance.ScoreCount
            };

            LastGameState = state;
        }

        public void ExecuteUndo()
        {
            if (!LastGameState.HasValue) return; // No move to undo

            GameState state = LastGameState.Value;

            //GameManager.Instance.LivesAmount = state.livesAmount;
            ScoreManager.Instance.ProcessScoreUndo(state.scoreAmount);


            // Clear the saved state since we've used it
            ClearUndoState();

        }

        public void ClearUndoState()
        {
            LastGameState = null;
        }

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
