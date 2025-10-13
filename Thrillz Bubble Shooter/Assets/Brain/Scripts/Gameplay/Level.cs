using Brain.Audio;
using Brain.Core;
using Brain.Managers;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Brain.Gameplay
{
    public class Level : MonoBehaviour
    {
        public event UnityAction<bool> OnLevelCompleted;

        private bool _isComplete = false;
        private bool _isRevealed = false;
        //public bool IsComplete { get { return _isComplete; } }

        public void Init()
        {
            _isComplete = false;
            _isRevealed = false;
        }

        public void RevealLevel()
        {
            if (_isComplete || _isRevealed) return;
            _isRevealed = true;

            ToggleBoard(true);
        }

        public void HideLevel()
        {
            if (_isComplete) return;
            _isRevealed = false;

            ToggleBoard(false);
        }

        public void ToggleBoard(bool toggled)
        {
            // Implement board toggling logic here
        }

        private void HandleLevelCompleted(bool success)
        {
            if (_isComplete)
                return;

            _isComplete = true;
            Debug.LogFormat("Completed Level!");

            if (OnLevelCompleted != null)
                OnLevelCompleted(success);
        }
    }
}