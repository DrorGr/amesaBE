namespace AmesaBackend.Shared.Events
{
    /// <summary>
    /// Interface for publishing events to EventBridge
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes a domain event to EventBridge
        /// </summary>
        /// <typeparam name="T">Event type</typeparam>
        /// <param name="event">Domain event to publish</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : DomainEvent;
    }
}

