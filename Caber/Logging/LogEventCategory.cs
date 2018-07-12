namespace Caber.Logging
{
    public enum LogEventCategory
    {
        /// <summary>
        /// Uncategorised event.
        /// </summary>
        None = 0,
        /// <summary>
        /// The event relates to loading and validation of configuration.
        /// </summary>
        Configuration,
        /// <summary>
        /// The event relates to authorisation of this instance or that of its peers.
        /// </summary>
        Security,
        /// <summary>
        /// The event relates to receipt of messages from a peer.
        /// </summary>
        Receivers,
        /// <summary>
        /// The event relates to sending messages to another peer.
        /// </summary>
        Senders,
        /// <summary>
        /// The event relates to routing of file propagation events internally.
        /// </summary>
        Routing,
        /// <summary>
        /// The event relates to local storage of data.
        /// </summary>
        Storage,
        /// <summary>
        /// The event relates to the service's lifecycle.
        /// </summary>
        Lifecycle,
    }
}
