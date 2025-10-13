using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Brain.Core
{
    /// <summary>
    /// Serves as a base class for the State pattern.
    /// </summary>
    public class State<T> where T : System.Enum
    {
        public T Phase { get; set; }
        public UnityAction OnPhaseStarted { get; set; }
        public UnityAction OnPhaseEnded { get; set; }

        public State(T phase, UnityAction onPhaseStarted, UnityAction onPhaseEnded)
        {
            Phase = phase;
            OnPhaseStarted = onPhaseStarted;
            OnPhaseEnded = onPhaseEnded;
        }
    }
}