using System;
using System.Collections.Generic;

namespace SpieleMarmelade.Core.StateMachine
{
    /// <summary>
    /// Minimal explicit state machine for <see cref="GameState"/>. No logic outside of the
    /// registered states should drive global flow transitions - everything funnels through
    /// <see cref="ChangeState"/>.
    /// </summary>
    public sealed class GameStateMachine<TContext>
    {
        private readonly Dictionary<GameState, IGameState<TContext>> _states = new();
        private IGameState<TContext> _current;

        public GameState CurrentState => _current?.StateId ?? GameState.Boot;

        /// <summary>Raised after a transition completes: (previous, next).</summary>
        public event Action<GameState, GameState> StateChanged;

        public void RegisterState(IGameState<TContext> state)
        {
            _states[state.StateId] = state;
        }

        public void ChangeState(GameState next, TContext context)
        {
            if (!_states.TryGetValue(next, out IGameState<TContext> nextState))
            {
                throw new InvalidOperationException(
                    $"GameStateMachine: no state registered for '{next}'.");
            }

            GameState previous = CurrentState;
            _current?.Exit(context);
            _current = nextState;
            _current.Enter(context);
            StateChanged?.Invoke(previous, next);
        }

        public void Tick(float deltaTime, TContext context)
        {
            _current?.Tick(context, deltaTime);
        }
    }
}
