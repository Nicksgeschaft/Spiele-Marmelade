namespace SpieleMarmelade.Core.StateMachine
{
    /// <summary>
    /// A single state in a <see cref="GameStateMachine{TContext}"/>.
    /// </summary>
    /// <typeparam name="TContext">The object the state operates on (e.g. GameManager).</typeparam>
    public interface IGameState<TContext>
    {
        GameState StateId { get; }

        void Enter(TContext context);
        void Exit(TContext context);
        void Tick(TContext context, float deltaTime);
    }
}
