# Design: Routing

* Up: [Design](Design.md)

The Routing system dequeues file modification events and dispatches them to
senders.

Broadly speaking, the loop works as follows:
1. Dequeue event.
2. Determine if any senders need this file. If not, discard the event.
3. Stat the file. We need the size at least.
4. Hash the file up to the length we just got.
5. Pass the event to each relevant sender.
6. On retryable error, re-queue the event.

Senders are responsible for tracking whether or not they've successfully
synced a specific `{QualifiedPath,stat,hash}` entry.

Routing is generally the link between our local state and the upstream
servers' state, ie. it is also the component which the expiry loop will ask
about replication targets.
