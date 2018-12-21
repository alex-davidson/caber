# Caber, Design Document

* [Protocol](Design.Protocol.md)
* [Storage and File System](Design.FileSystem.md)
  * [Filtering](Design.FileSystem.Filters.md)
* [Local Data Storage](Design.LocalStorage.md)
* [Configuration Diagnostics](Design.ConfigurationDiagnostics.md)
* [Authorisation and Identity](Design.Authorisation.md)

## Analysis

**Given one or more services spread over multiple machines, each logging to
local storage, gather up those log files and forward them to another machine.**

**Store-and-Forward:** We do not want to modify the existing services to log
to a network endpoint. This is partly because we may not have any control over
their configuration, but mostly because logging to local storage is a feature,
not a bug, since it is likely to be durable and available even when the
network is not.

**Log Expiry:** The owning service may not support log expiry, or may not have
it configured. Keeping thousands of old logs in sync is going to add overhead,
so provide an expiry feature. Expiry should in any case be controlled by Caber
because it knows which files have been fully synced with target servers.
It would be 'nice' to enable expiry by default on any fileset which is being
synced, but this leads to risky behaviour when OS files exist in such a
fileset. What if the fileset is intended as a receiver target, not for
replication, and then someone adds a route and sender for it? The behaviour
changes, in a dangerous and non-obvious fashion. We could exclude known
'system' directories but that is likely to lead to more strange edge cases.
For example, services may log to their own log directory under Program Files.
Is it correct to expire logs in this case, despite being within a 'system'
directory hierarchy? *Log expiry must be explicitly requested, though a
default interval may be assumed.*

**Service Hierarchy:** We may want to forward logs more than one hop across
the network. In a situation where the destination lies on the other side of
a firewall, it may be necessary to gather logs onto a specific machine before
forwarding them beyond the firewall. However, there is no reason for any
single instance to know about this. *Each instance should only know about its
own sources and destinations.*

**Content Referencing:** To determine whether a file has been sent, we shall
consider its hash. Since Caber cannot know if a file is still in use, we need
to propagate changes to a file as it's extended. Therefore we need to consider
its size as well, and permit 'updating' an already-sent file iff the new data
is purely an append. Tolerating arbitrary changes to already-sent data would
risk loss of data as a buggy or malicious sender could overwrite safely-sent
content. *Changes to existing data will never be tolerated, only appends.*

Note that this means that log rollover schemes which involve moving old logs
and using the same name for the current log cannot be supported. There really
is no sane way to tolerate such systems without permitting arbitrary
overwrites, which is unsafe, therefore no effort will be made to support these
schemes. Use datestamped log file names instead.

(Possibly enable an alternative mode where files are *identified* by their
hashes, sync is always forced, and files are renamed as appropriate to reduce
wire traffic? Out of scope for now.)

**Authentication:** Instances which communicate must mutually authenticate.
The simplest way to do this is X509 certificates, but then we have a
certificate management problem: updating unique certificates across a dozen
machines is an annoyance, especially if we do not have sufficient access to
the platform's own certificate distribution services, or if we're crossing
domain boundaries. *From the user's perspective, it should be possible to set
up the Caber service once and have it Just Work(TM) thereafter.* Since the
service network is basically hierarchical, it would be reasonable to use
conventional X509 to secure instances 'higher up' the tree, but leaf nodes may
be numerous and difficult to manage. Therefore we will fall back to generating
self-signed X509s, managed internally by each instance and authorised at the
other end as part of initial setup. These pairs can be refreshed periodically
and the existing trust relationship used for public key distribution, ie. one
trusted instance A notifies another instance B that it should trust a new
certificate and switch to using it as soon as possible. An overlap of a week
should usually be sufficient. *We will use X509 certificates, self-signed if
not configured, authorised as part of setup, and provide tools to simplify
this process.*

**Authorisation:** Just because an instance's identity is known, doesn't mean
it should be allowed to push just anything. Each instance will have a list of
named filesets which it pushes, and the receiving instance must only accept
those which it expects to get from that sender. *Authorisation should be easy
to set up.* When an instance initially registers with a receiver it should
indicate which filesets it wishes to submit as well. *Authorising a sender's
identifying certificate is part of the same workflow as authorising the
submission of each of its filesets.*

## General Design

Each Caber instance has:
* configuration:
  * a human-readable name, which does not need to be unique.
  * a shortcode, which is a string of arbitrary characters used to
    disambiguate the human-readable name.
  * one or more storage hierarchies.
  * zero or more receivers.
  * zero or more senders with targets.
* a local database containing:
  * the instance's UUID.
  * the instance's self-generated certificates.
  * the identities and authorisations of its peers.

A storage hierarchy is a directory with a configured name and a set of rules.

A receiver accepts file information posted from elsewhere and puts it in the
specified storage hierarchy.

A sender receives events from storage hierarchies and syncs related file
information with a target location, which may be a filesystem location or an
HTTPS URI (typically another Caber instance).

All Caber instances in a network need to agree on a hash algorithm. For
simplicity, SHA256 will be used universally.

### Identification

A Caber instance is uniquely identified by a UUID, but these are cumbersome
to type. Therefore each one also has a human-readable name and a shortcode,
which may be used in place of a UUID.

### Authentication and Authorisation

If no certificate is configured, the instance should generate a self-signed
one.

At start time, or in the event of a request failing due to an authentication
error, the instance's senders should attempt to 'register' with their target
endpoints. This involves a verifying the target's identity, then requesting
authorisation to post a set of storage hierarchies to it.
1. If the target's X509 certificate is not trusted, this fact should be logged
   and the sender inactivates until the issue is remedied.
2. Once the target is trusted, a registration request is made with the
   following information:
   * This instance's UUID, name, shortcode and public key.
   * The UUID we think we're talking to, if this is not first contact.
   * A list of storage hierarchies this sender will try to push to this target.
3. If this instance is not trusted by the target, it responds with HTTP 401
   and logs the request.
4. Once this instance is trusted by the target, the response will contain:
   * The target's actual UUID, name, and shortcode.
   * The subset of storage hierarchies this sender is authorised to push.

It must be possible to explicitly deauthorise an instance, since otherwise a
rogue instance could flood our logs with requests for authorisation. Logged
requests should also be rate-limited; there's no value in raising an
operational event for every attempted registration.

### Local Database

Configuration must be considered immutable by the Caber application, but it is
necessary to record some mutable local state to track identities and
authorisation of peers, as well as any locally-generated certificates.
Security must be considered here, since the local certificate must include the
private key.

This local state will be recorded in a filesystem hierarchy, using filesystem
security capabilities to control access:
* The user account under which Caber runs: owner, full control.
* Designated administrator user/group: read, write, modify.
* Administrators group: full control.
* Everyone else: no access.

The Caber service should verify the above rules at startup and warn if they do
not match. The administrator user/group should be an element of configuration.
The expectation is that the Caber CLI tool, used for authorisation, is run by
the designated administrator.

### Storage Hierarchies

Storage hierarchies are linked to senders via routes.

Storage hierarchies must never overlap. This is hard to enforce due to the
existence of symlinks and junction points, but we can warn about simple
misconfigurations and strange observations.

A storage hierarchy need not be a single simple directory. Additional
directories may be mapped into it, as long as they never overlap with each
other or the other storage hierarchies.

We attempt to *preserve case* of hierarchy names, but ultimately our ability
to do this reliably will depend on the receiving filesystem. To disarm some
obvious footguns, hierarchy names are not permitted to differ only by case;
while they are compared case-sensitively, two hierarchy names differing only
by case is always considered a configuration error.

Storage hierarchies can have expiry rules.
* Expiry is never enabled by default.
* A hierarchy with no routes is never propagated elsewhere, and files
  should never expire. Configuring expiry on such a hierarchy will cause a
  warning to be logged, but it will be enabled.
* A hierarchy with routes should only expire files which have been safely
  propagated along all routes. Files which have not been propagated will not
  be cleaned up, even if they have expired. Warnings may be logged if a file
  lives beyond its expiry time due to replication failure.

Storage hierarchies should not fire events too often. There should be a
minimum interval (configurable) between a file sync being performed and the
same file being synced again.

There exists a special hierarchy called `.caber`. This contains the Caber
instance's own log files and monitoring data within a subdirectory named
using the instance's unique identifier. It exists even if not configured.
*Special facilities may be provided for silently shipping this up the tree
of Caber services (TBD).*

#### Files

A file is identified by:
* The name of the hierarchy it belongs to.
* Its 'effective' path relative to that hierarchy's root, after
  mapping/grafting/etc.

The path is normalised to UTF-8 and a forward-slash-separated form.

A file's state refers to a point in time. There is no such thing as 'current'
state, just the last one we know about. State consists of:
* Length.
* The hash of the file up to that length.
* Last modification date.

Since the file may be appended to between the derivation of the hash and
actually sending the data to the target, we must know how long it was at the
time so we can send only that many bytes.

The modification date is used for implementing expiry.

### Receivers

A receiver is an HTTPS endpoint which understands the Caber JSON protocol.

A receiver has a single target storage hierarchy.

### Senders

A sender has a single target URI.
* If this refers to an HTTPS endpoint, it must understand the Caber JSON
  protocol.
* If this refers to a filesystem location, the sender must behave as if it is
  sending to another Caber instance which is receiving into that filesystem
  location.

### Routes

A route links one or more storage hierarchies to one or more senders.

A route may have a replication target, which defaults to 100% and cannot be
zero. This dictates how many of the senders must have fully acknowledged
receipt of a given file before the route considers the file to have been
propagated.

### Logging and Diagnostics

Caber's own logging consists of operational and diagnostic events, falling
into the following main categories:
* Configuration: Always operational, since configuration is immutable as far
  as the application is concerned.
* Security: Mostly operational events, eg. services needing authorisation.
  Explicit deauthorisation of a service should prevent its clamouring from
  appearing in operational logs, but it may still go to diagnostics.
* Receivers: Mainly diagnostics, eg. malformed requests.
* Senders: Mainly diagnostics, eg. timeouts, DNS lookup failures.
* Routing: Mainly diagnostics, eg. failure to achieve replication targets
  after X attempts.
* Storage: Mainly diagnostics, eg. failed to lock file. Low disk space
  warnings appear as operational notifications.

Additional categories for completeness:
* None: A catch-all for log events which sneak in elsewhere when funnelling
  everything through a general logging framework such as log4net.
* Lifecycle: Runtime infrastructure events, eg. starting the service.

Operational logging occurs at WARNING or ERROR level and is always switched
on, since it indicates a need for manual intervention. By that same token,
anything which *doesn't* need manual intervention to fix must never go in the
operations log.

Diagnostic logging occurs at INFO or DEBUG level. It should be turned off by
default. Distinguishing intermittent failures from operational failures is
Hard(TM) so it's possible that misconfigurations and environmental problems,
although in need of manual rectification, will end up in here rather than in
the operations logs, but the expectation is that such issues will eventually
generate operational messages anyway. For example, a file living past its
expiry time on server A because target server B is unable to write to its
storage area, due to filesystem security misconfiguration.

Caber logs must be machine-parseable. This will be achieved by logging to two
places: a human-readable plaintext log, and a machine-readable JSON log.

Caber should also log metrics information. This will be achieved using
Metrics.NET and a custom periodic logger to record JSON snapshots. By default
only Caber's own metrics will be logged, but there will be options to enable
general Application and System performance counters too.
