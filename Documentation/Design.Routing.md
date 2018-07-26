# Design: Routing

* Up: [Design](Design.md)
* Related: [Routing](Design.FaultManagement.md)

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
about replication targets. Routing is also responsible for maintaining
caches of file snapshots, etc as necessary.

## Snapshots and Caching

When we require stat and hash information for a qualified path, we ask the
snapshot service to acquire this information about the current on-disk state
and timestamp it with 'now'.

A change event has a timestamp. If this timestamp is later than that of the
last snapshot, we must update the snapshot, otherwise it can be reused as
there's no reason to believe that it's out of date.

Since the event queue should be replacing outdated entries when new change
events come in, this means that we can handle a transient failure efficiently
by simply requeuing the event, as the snapshot acquired the first time around
will be reused if no new events arrive for that qualified path.

## Replication Targets

The core routing loop knows about a set of receiving routes. Each of these
usually maps to a single sender with a trivial definition of 'synced
successfully'.

These receiving routes are expected to deal with replication targets. If one
has multiple senders, it needs to trust them each to track their remote state
separately, and only report 'replicated' to the routing loop when enough of
them claim to be synced.

A multi-sender route should never perform early bail-out. Only senders are
allowed to do this. The route must try every sender so that, where possible,
100% replication is achieved even if the required target is reached sooner.

## Expiration

The expiration loop iterates files within the storage area subject to
configured expiry policies. When an expiry candidate is found:

1. An exclusive lock is taken.
1. A snapshot of the locked file is taken.
1. The router is asked whether all routes have met their replication targets.
1. If so, the file is deleted and the lock released.
