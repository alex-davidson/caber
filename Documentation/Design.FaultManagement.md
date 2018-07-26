# Design: Fault Management

* Up: [Design](Design.md)

There are basically three ways in which something can fail:
* A recognised fault which can be remedied by the software itself. For
  example: an attempt to send not enough of a file, so the recipient rejects
  it as unverifiable. This can be fixed by sending the whole file, or updating
  our record of how much the recipient already has.
* A transient fault, probably environmental. This cannot be 'fixed'
  automatically per se but we can retry a few times before bothering the
  user about it.
* A permanent or 'latching' fault, which won't go away on its own and cannot
  be safely worked around. For example, a recipient reporting a conflict
  between what we're sending and what it already has.

Recognised faults should be fixed automatically in every case. If fixing a
recognised fault can get us into an infinite loop, then either we should not
be trying to resolve it automatically (promote to transient or permanent
fault) or we need to reconsider how we fix it.
Recognised faults should never be logged to Operations, only Diagnostics
(unless automatic remediation fails, in which case it's not a recognised fault
any more).

Transient faults need a retry counter. We should never block potentially-
-successful operations on these, so typically the failed operation should be
requeued. Sometimes it may be difficult to determine if the fault is caused
by the operation or its parameters, eg. a send attempt timing out for one
particularly large file vs. timing out for every operation. Distinguishing
these cases will require careful consideration.
Once a transient fault's retry counter crosses a threshold, it should be
reported to the Operations log.

Permanent faults should immediately be recorded in the Operations log and no
retries attempted. Retrying a permanent fault should be considered a bug, if
it can be reliably identified as such.

When the class of fault is not yet known, it should be treated as permanent.
It will thus result in Operations log entries and potentially bug reports,
which will cause it to be categorised and dealt with.

## Retries

Transient faults can hang around for a while sometimes. If a retryable
operation fails quickly, the retry counter might become exhausted within
milliseconds.

Therefore there needs to be a back-off period between retries.

Sometimes the exact parameter of a fault may not be clear. For example, if
sending a file to a destination fails, is that something to do with the file
or the destination?

Multiple-fault scenarios complicate things still further. Given a file which
needs to be sent to multiple destinations, with different back-off times and
existing failure counts, when should the next retry be scheduled given that it
will be dispatched to all handlers at the same time?

Let us define a tracker object which applies to an operation regarding a
specific context (a service, an event, etc) and has a simple contract:
```
bool TryProceed(out RetryToken token);
RetryToken Fault();
void Success();
```
`TryProceed` is called before the operation. If it returns `true` the
operation shall proceed immediately, ie. the context is not in a failed state
or the retry back-off period has elapsed. If it returns `false` the `token`
represents the remaining back-off period.

The `Fault` and `Success` methods are called to record the outcome of the
operation. `Fault` returns a `RetryToken` representing the back-off period.

The `RetryToken` is composable in two ways:
* A service and its parameter should combine their tokens with a *logical AND*
  operator, representing the expiry of both back-off periods.
* A set of independent services should compose their tokens with a *logical
  OR*, representing the expiry of any of their back-off periods.

`RetryToken`s may be waited upon or tested for expiry directly, much like
`Task`s (in terms of which they may well be implemented).

Note that it makes no sense for a parameter value itself to have a tracker.
The same value may be passed to multiple services, some of which process it
successfully and some of which don't. Tracking failures only makes sense for
a *process*, defined as a combination of *operation* and *input*. Thus, it is
the job of a service to record trackers for significant parameters. This
obviously leads to concerns over object lifetimes since a simple dictionary
will retain its keys forever. Appropriate solutions for this depend on the
specific context.

## Blame

Tracking failures of a service and its parameters separately means that a
failure of the service may also be reported as a failure of all parameters
seen in that interval, if done naively.

This should depend on how things are recorded. A failure which is clearly due
to the parameter should be recorded as such, incrementing only that counter.
This means that the service's tracker won't necessarily have either `Fault` or
`Success` called for an operation which fails due to the parameter. *The
tracker is not notified of the completion of every operation.*

But what if the final straw for both comes from the same operation? Should
this even be possible? Should we account for 'unknown faults' against both, or
neither? *For now, this case shall be left undefined, although I will lean in
favour of blaming the service.*

## Reporting

If our back-off periods are long enough, there's little reason to ever give up
entirely on trying to recover from a transient fault. We do however have to
decide when to notify the user.

This may be after a period of time or after a number of retries. Given a
simple fault these are the same thing, since a constant number of retries
should equate to constant time, but if multiple types of retry are involved
the duration may be unpredictable.

## API

Generally a service operation will either succeed or fail. Retries are a
concern of the caller, therefore supporting them should not be onerous for
the service.
