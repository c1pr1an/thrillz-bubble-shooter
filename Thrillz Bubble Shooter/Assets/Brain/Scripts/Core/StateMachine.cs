using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Brain.Core
{
    /// <summary>
    /// Simple implementation of State machine.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StateMachine<T> where T : System.Enum
    {
        public event UnityAction<T> OnStateChanged;
        private List<State<T>> _states;

        private T _currentPhase;
        public T CurrentPhase { get { return _currentPhase; } }

        private string _logTag;
        public string LogTag
        {
            get { return _logTag; }
            set { _logTag = string.IsNullOrEmpty(value) ? "StateMachine" : value; }
        }

        public StateMachine(string logTag = null)
        {
            _logTag = string.IsNullOrEmpty(logTag) ? "StateMachine" : logTag;
            _states = new List<State<T>>();
        }


        public void AddState(State<T> state)
        {
            if (_states.Exists(s => EqualityComparer<T>.Default.Equals(state.Phase, s.Phase)))
            {
                Debug.LogWarningFormat("[{0}] : state [{1}] already exists. Skiping..", _logTag, state.Phase.ToString());
                return;
            }

            _states.Add(state);
        }

        public void Removestate(T phase)
        {
            _states.RemoveAll(s => EqualityComparer<T>.Default.Equals(phase, s.Phase));
        }

        public void ChangeState(T phase, bool allowSameState = false)
        {
            if (allowSameState && EqualityComparer<T>.Default.Equals(phase, CurrentPhase))
            {
                Debug.LogFormat("[{0}] : Already in phase [{1}]. Skipping...", _logTag, _currentPhase.ToString());
                return;
            }

            State<T> currentState = _states.Find(gs => (EqualityComparer<T>.Default.Equals(_currentPhase, gs.Phase)) && (gs.OnPhaseEnded != null));

            Debug.LogFormat("[{0}] : Exit Phase : [{1}]", _logTag, _currentPhase.ToString());
            if (currentState != null)
                currentState.OnPhaseEnded();


            _currentPhase = phase;
            RaiseStateChanged();

            Debug.LogFormat("[{0}] : Enter Phase : [{1}]", _logTag, _currentPhase.ToString());

            State<T> nextState = _states.Find(gs => (EqualityComparer<T>.Default.Equals(phase, gs.Phase)) && (gs.OnPhaseStarted != null));
            if (nextState != null)
                nextState.OnPhaseStarted();
        }

        private void RaiseStateChanged()
        {
            if (OnStateChanged != null)
                OnStateChanged(_currentPhase);
        }
    }
}