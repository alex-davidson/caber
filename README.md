# Caber

A service for gathering logs and other append-only files and securely shipping
them to a central site.

* [Design](Documentation/Design.md)
  * [Protocol](Documentation/Protocol.md)
  * [Storage and File System](Documentation/Design.FileSystem.md)
    * [Filtering](Documentation/Design.FileSystem.Filters.md)
  * [Configuration Diagnostics](Documentation/Design.ConfigurationDiagnostics.md)
* [Configuration Example](Documentation/Example.md)
* [MIT License](LICENSE.txt)

_What it ~~does~~ will do:_

* Synchronises append-only log files between computers or directories.
* Ensures that only appends are propagated, ie. damage to an already-shipped
  file will be recorded but not propagated.
* Uses strong encryption to authenticate service instances and transport
  files.

_What it doesn't:_

* Any kind of configuration management. Caber instances are not a cluster or
  a managed system: they're just independently-configured services which
  shuttle files around.
* Operate on Linux, Mac, etc. Windows-only for now.
  * However, the code will try not to depend heavily on Windows ways of doing
    things, eg. case-insensitive filenames, so supporting other platforms is a
    consideration.

## Progress and Scope

1. Implement storage hierarchies.
   * ~~Configurable roots.~~
   * Listeners and notifications.
   * File state tracking. Data structures, hashing service, etc.
   * ~~Tree mapping/grafting.~~
   * Expiry.
   * Rate-limits.
1. Authentication/authorisation mechanisms with tests.
   * Add HTTP wrapper/integration.
   * Add filesystem storage.
   * Record pending authorisations.
   * CLI tool, for managing filesystem storage.
   * Add support for external X509 certs.
1. Sender implementation.
   * Implement 'register' operation.
   * Implement 'compare' operation.
   * Implement 'write' operation.
   * Implement 'append' operation.
   * Add HTTP wrapper.
1. Receiver implementation.
   * Implement 'register' operation.
   * Implement 'compare' operation.
   * Implement 'write' operation.
   * Implement 'append' operation.
   * Add HTTP wrapper.
1. Simple configurable routing.
   * Integrate with expiry.
   * Add multicast routing.
   * Add replication targets.
1. Support local sender targets (filesystem URIs). No authnz required.
1. Integrate senders and receivers with authnz.
1. Event loops/scheduling.
   * Handle sender state transitions (authnz failures, etc).
   * Integration testing. Fuzz-testing.
1. Logging and metrics.
   * ~~Debug logging.~~
   * ~~Basic operational logging.~~
   * ~~Structured event logging.~~
   * Configure logging.
   * Special handling of the .caber hierarchy.
1. (other?)

## Why 'Caber'?

It was the first word which came to mind when I started thinking about
throwing logs around.
