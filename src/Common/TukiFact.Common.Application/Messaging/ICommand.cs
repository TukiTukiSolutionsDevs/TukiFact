namespace TukiFact.Common.Application.Messaging;

/// <summary>
/// Optional marker for state-changing requests whose type name does not end in <c>Command</c>.
/// Commands (marker or <c>*Command</c> suffix) run inside <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/>.
/// </summary>
public interface ICommand;
