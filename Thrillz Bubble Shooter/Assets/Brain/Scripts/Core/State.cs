using UnityEngine.Events;

namespace Brain.Core
{
    public class State<T> where T : System.Enum
    {
        // Properties
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