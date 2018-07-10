# Protocol

* Up: [Design](Design.md)

Two types of request: control message and stream.
* Control messages are always JSON using UTF-8 encoding.
* Streams are used to send bulk binary data, ie. file content.

Both types of request define the following headers:
* X-Caber-Operation: operation name. Should be redundant, but provided for
  debugging eg. mangled URLs.
* X-Caber-Sender: UUID of the sender.
* X-Caber-Recipient: UUID of the intended recipient.

**At present, authentication is always handled via mutual certificate exchange.
Tokens may be added for performance reasons in the future.**

Operations which mutate data **MUST ALWAYS** roll back if they are unable to
return `HTTP 200 OK`.

An `HTTP 400 Bad Request` response generally means that whatever the client is
trying to do doesn't agree with what the server thinks is possible, ie. their
state doesn't match. This might be recoverable by trying a more conservative
operation (posting more of the file) or by comparing notes with the server to
re-sync states.

An `HTTP 401 Unauthorized` response indicates that the client is not
authorised to do whatever it tried to do. If it's not currently in the process
of trying to register with the server, it should abort any other requests to
this server and re-register.
The client should not attempt requests for which it *expects* an `HTTP 401
Unauthorized`, eg. trying to push a hierarchy which it was told is not
permitted.

An `HTTP 409 Conflict` response indicates that the client is sending data
for a file or byte range which the server believes it already has, and the two
copies do not match. This is not retryable and cannot be resolved without
extensive manual intervention. It likely indicates a broken invariant
somewhere.

An `HTTP 500 Internal Server Error` is a catch-all for an unexpected failure.
These should be considered retryable, but backing off after a series of such
failures is strongly advised.

An `HTTP 501 Not Implemented` means that the server does not support whatever
the client asked for. During registration this might happen if they do not
agree on a hash algorithm, but this should never happen since everyone should
be using SHA256...

## Data Types

Type definition syntax is Typescript.

Basic types:
* `Guid`: a string containing a Guid, in 8-4-4-4-12 form.
* `Base64String`: a string containing a Base64-encoded sequence of bytes.
* `CountOfBytes`: a 64-bit integer formatted as a string.
* `FilePath`: a string containing a normalised path.

Structured types:
```
interface CaberIdentity {
    uuid: Guid;
    name: string;
    code: string;
}

interface FileState {
    hash: Base64String;
    length: CountOfBytes;
}
```

## Registration with Target

Control message. Client registers its identity with the server and learns
which hierarchies it is allowed to push.

### Request
```
POST ~/register/<client-uuid>
...
X-Caber-Operation: register
...
{
    clientIdentity: CaberIdentity,
    // Server's identity as believed by the client:
    serverIdentity?: { uuid: Guid; },
    // Environment description, which must match the server's.
    environment: {
        hashAlgorithm: "SHA256"
    },
    // Storage hierarchies we'd like to push:
    roots: { name: string; }[]
}
```

### Responses

#### 200 OK
```
{
    serverIdentity: CaberIdentity,
    // Storage hierarchies we'll accept:
    acceptedRoots: { name: string; }[]
}
```

#### 401 Unauthorized
No response body.

#### 501 Not Implemented

May be returned if the environments are incompatible, eg. hash algorithm
differs.

```
{
    serverIdentity: CaberIdentity,
    // Server's environment description, which is incompatible with the client's.
    environment: {
        hashAlgorithm: "SHA256"
    }
}
```

## Compare State with Server

Control message. Client tells the server which files it has and what state
they're in. Server responds with its own state for those files which it has.

### Request
```
POST ~/compare/<client-uuid>/<root-name>
...
X-Caber-Operation: compare
...
{
    clientIdentity: CaberIdentity,
    root: string;
    files: {
        path: FilePath;
        state: FileState;
    }[];
}
```

### Responses

#### 200 OK
```
{
    serverIdentity: CaberIdentity,
    root: string;
    files: {
        path: FilePath;
        state: FileState;
    }[];
}
```

## Write Entire File

Stream operation. Client writes an entire file to the server.

### Request
```
POST ~/write/<client-uuid>/<root-name>/<file-path>
...
X-Caber-Operation: write
...
[Stream content]
```

### Responses

#### 200 OK / 409 Conflict

```
{
    serverIdentity: CaberIdentity,
    root: string;
    // State which the server actually has:
    file: {
        path: FilePath;
        state: FileState;
    };
}
```

## Append to Existing File

Stream operation. Client appends content to a file which the server already
has.

### Request
```
POST ~/append/<client-uuid>/<root-name>/<file-path>
...
Range: bytes=<start>-
X-Caber-Hash-Existing: <hash of starting content>
X-Caber-Hash-New: <hash of content after append>
X-Caber-Operation: append
...
[Stream content]
```

### Responses


#### 200 OK
```
{
    serverIdentity: CaberIdentity,
    root: string;
    // State which the server actually has:
    file: {
        path: FilePath;
        state: FileState;
    };
}
```

#### 400 Bad Request

May be returned if:
* Server does not yet have <start> bytes.
* `X-Caber-Hash-Existing` hash does not match server's copy of <start> bytes.
* `X-Caber-Hash-New` hash does not match server's copy, post-append.

```
{
    serverIdentity: CaberIdentity,
    root: string;
    // State which the server actually has:
    file: {
        path: FilePath;
        state: FileState;
    };
}
```

#### 409 Conflict

Returned if the new content conflicts with what the server already has in some
way.

```
{
    serverIdentity: CaberIdentity,
    root: string;
    // State which the server actually has:
    file: {
        path: FilePath;
        state: FileState;
    };
}
```
