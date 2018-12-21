# Authorisation and Identity

Each instance is identified by a UUID and authenticated using an X509
certificate. These certificates are generated locally with a configured expiry
time, defaulting to 12 weeks. After 10 weeks (2 weeks until expiry) an
instance should generate a new certificate and notify its peers on all
subsequent communication of the new thumbprint, which they should then trust
if they trust the existing one.

If a peer relationship does not require communication for an entire week, the
client should ping the server if only to check that it's alive. Consider doing
this daily, or even more frequently.

For initial communication, an instance may additionally identify itself with a 
name/shortcode pair. The target peer may use these to look up pre-provided
authorisation info.

## Database

The local database contains authorisation information for the instance and all
its peers.

The database consists of three major parts:
* Observed: Each node which is observed will supply its UUID, its
  name/shortcode pair, and X509 certificate. All observed nodes are locally
  identified by UUID and all associated info is filed under that.
* Specified: Information provided by the user is stored here. It is usually
  filed by name/shortcode and is unlikely to be complete, but it does act as
  a sanity check: if the Observed universe ever conflicts with the Specified
  universe, the operation in context should immediately be aborted with a
  message.
* Provisional: Information provided by the user to 'prime' bits of the system
  is stored here. This includes things like initial X509 thumbprints for
  pre-authorising peers. Once used, the system should update its records in
  the Observed universe and remove the superseded Provisional information.
  This may be keyed by various things depending on the purpose of the data.

## Authentication

First contact with any peer should involve the exchange of UUID, name,
shortcode, and thumbprint. The thumbprint MUST match that of the X509
certificate in use by that peer.

1. Validate the peer's certificate, ie. don't give away anything until they
   prove that they have the private key for the certificate they present.
   This should be done by our HTTP library.
2. Look up the name/shortcode in the Specified section.
   * If not present, fail.
   * If present but disabled, fail.
3. Look up the thumbprint in the Provisional section.
   * If present but name and shortcode don't match, fail.
4. Look up the UUID in the Observed section. If present:
   * If the name and shortcode don't match, fail.
   * If the thumbprint is in-date, succeed.
   * If the thumbprint is in the Provisional section, succeed.
5. If the certificate's Issuer is apparently a Caber instance:
   * If the Issuer and Subject don't match, fail.
   * If the Issuer's UUID and name don't match the request, fail.
6. If the certificate is not self-signed, look up the thumbprint of each
   certificate in the chain in the Provisional section:
   * If any are blacklisted, fail.
   * If any are present with a wildcard grant, succeed.
7. Fail.

If we haven't failed the authentication request:

1. Create an entry for the UUID in the Observed section if not present.
2. Record name, shortcode against UUID in the Observed section.
3. Add thumbprint to this UUID's known ones in the Observed section.
4. Remove thumbprint from the Provisional section.

## Authorisation

1. If authentication fails, we must not get this far.
2. Look up authorisation rules in the Specified section by UUID.
3. Look up authorisation rules in the Specified section by name and shortcode.
4. If any applicable Deny rules exist, Deny.
5. If any applicable Allow rules exist, Allow.
6. Deny.

## Certificates

Every Caber instance will generate an identifying X509 certificate for itself.

Subject: O=service,O=caber,UID=<uuid>/CN=<name>
Issuer: O=service,O=caber,UID=<uuid>/CN=<name>
NotValidBefore: 5 minutes before 'now' on the creator machine
NotValidAfter: 12 weeks after 'now' on the creator machine

These certificates may be installed into the service user's own certificate
storage (on Windows) so that the service itself need not worry about securing
them itself. Expired certificates belonging to the service should be
automatically deleted.

The start of the validity period is backdated by five minutes to account for
clock skew. Five minutes of skew is rather excessive, but it is the default
maximum tolerated by Active Directory's Kerberos implementation.

By default, Caber's generated certificates will use 2048-bit RSA keys and the
SHA512 hash algorithm. Caber will not yet impose this as a verification
requirement though: it is entirely up to the platform's own validation to
decide if a presented certificate is hard enough to forge. In the future this
may be tightened up, so it is recommended that 3rd-party implementations use
parameters at least as secure as Caber's default.

## Logging

Nearly all authorisation-related events qualify for operational logging, since
they cannot be safely resolved by the software itself.

Some events may be triggered by cheap brute-force attacks and are unlikely to
be produced by legitimate traffic. These should probably be demoted to
debug-only diagnostic logging, although a large number of these should be
logged operationally as a security concern.
