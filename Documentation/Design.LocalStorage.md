# Design: Local Data Storage

* Up: [Design](Design.md)

Caber operates with two main categories of storage:
* the [file hierarchies being synchronised](Design.FileSystem.md), and
* its own internal database.

Caber's internal database consists of multiple key-value stores and is used to
keep track of peer authorisations, certificates, etc.

At first glance an embedded database like SQLite could be used for this, but
there are additional concerns.

Modifying Caber's synchronisation rules requires modifying the configuration
file and restarting the service. This is because live updates to these
structures are awkward to achieve and would involve reinitialising the entire
service anyway.

But making changes to peer authorisations would be extremely clunky if done in
this manner. Caber needs to maintain records of UUID/name/certificate
associations anyway, so it makes sense to store authorisation info alongside.

Our administration tool therefore needs to communicate with the Caber service
in order to notify it of changes. This means we have to authenticate the
administration tool against the service somehow, but what if we merely
communicate via the filesystem? The platform's filesystem ACLs can be used to
control access.

Taking it a step further, why not simply allow the admin tool to update
Caber's on-disk data store directly?

Issues with this approach:
* Safely and atomically updating multiple files is hard without simply locking
  the entire database.
* The service needs to hit the disk regularly to get updated data.

But:
* We can avoid the need for atomic multi-file updates with careful design of
  the data model.
* The OS's filesystem caches should reduce the cost of repeatedly accessing
  the same files.

## Structure and Mechanism

Atomically replacing a single file can be done safely as long as writers do
not contend on it. Reading can be done without exclusive locks in most cases,
although read locks might cause writes to fail.

In the event of a crash mid-write we do not want to risk corrupting data.
Therefore we cannot naively overwrite the file. If we assume that deleting and
renaming/moving files are always atomic operations on filesystems we care
about, we can use a move-create-delete process to safely replace files.

This requires multiple files per key, therefore it makes sense to have one
directory per key and put the relevant files inside it. The database is not
expected to grow beyond a few dozen keys so we can use a simple flat
namespace.

Our database will be a simple key-value store, supporting only three
operations: Read, Write, Delete. Values can only be written in their entirety.
Atomic patching is not possible; the value must be read, modified, and
replaced, which will not be an atomic update.

### Writing a value

1. Acquire exclusive lock on key, by creating a delete-on-close lockfile.
2. Rename existing file.
3. Write out new file and force the OS to flush it.
4. Delete renamed (backup) file.
5. Release exclusive lock on key, by closing the lockfile.

The exclusive lock is required to prevent writers from trying to update
the same key simultaneously. While it may be possible to remove the need for
this lock, it's easier to understand the system if we simply ban concurrent
updates.

A failure during write may leave the key in various states depending on how
and when the process fails.
* We may be left with the backup file and no new one, in which case
  subsequent operations should consider the backup file to be 'good'.
* We may be left with the backup file and a corrupted new one. Again, the
  backup should be preferred.
* We may be left with the backup file and an intact new one. However, since
  the write is technically not complete until the backup is deleted, it is
  acceptable to treat this the same as the other cases and simply prefer the
  backup.

### Reading a value

Reads should not usually need to lock anything, but if an error occurs the
reader might need to do some recovery work, in which case it needs to acquire
an exclusive lock for the duration.

Fast path:
1. If the backup file exists, read it.
2. Otherwise if the main file exists, read it.
3. Otherwise no value available (default or empty).

If an exception is thrown by the above:
1. Acquire exclusive lock on key, by creating a delete-on-close lockfile.
2. Try to read the backup file.
   * If successful, return the value.
   * If the file is corrupt, delete it.
3. Try to read the main file.
   * If successful, delete the backup file and return the value.
   * If the file is corrupt, delete it.
4. Otherwise no value available (default or empty).

