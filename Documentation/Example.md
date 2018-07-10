# Configuration Example

Let's say you have some servers, with services logging locally:

* Server A:
  * nginx, logging to L:\logs\nginx\
* Server B:
  * Postgres, logging to D:\pglog\
  * Tomcat, logging to L:\logs\tomcat\
* Server C:
  * A node.js application, logging to E:\app\log\ and E:\app\perf\perf.txt

And the following concerns:
* HQ wants logs archived for a year when permissible, but Server A doesn't
  have enough disk for that.
* The node.js app doesn't do log cleanup and the E: drive keeps filling up,
  and you've not gotten around to writing a script to clean it up safely.
* Correlating those nginx traffic logs with the Postgres performance stats
  could be useful for diagnosing those load spikes on irregular Saturdays at
  11pm.

So you'd like to get all those logs onto server D, which has Logstash and
plenty of disk.

Let's start by deciding where we want things to go:
* D:\logs\nginx\ should contain server A's nginx logs, but only the anonymised
  ones (GDPR, after all).
* D:\logs\postgres\ should contain server B's Postgres logs.
* D:\logs\tomcat\ etc.
* D:\logs\some-app\ should get server C's application logs
* D:\logs\some-app\perf\ should get server C's application performance logs.

## Server A
```xml
<storage>
  <add name="nginx" path="L:\logs\nginx\">
    <filters>
      <match rule="exclude" />
      <match glob="**\*anonymised*" rule="include" />
    </filters>
  </add>
</storage>
<senders>
  <sender name="server-d" uri="https://server-d.domain.local/caber" />
</senders>
<routes>
  <route from="nginx" to="server-d" />
</routes>
```
* Declare a storage area called `nginx`.
  * Limit which files will actually be picked up, by filename.
  * Leave log expiry to the owning service.
* Declare an HTTPS destination, ie. another Caber instance or something which
  understands the protocol.
* Route files from the storage area to the destination.

## Server B
```xml
<storage>
  <add name="postgres" path="D:\pglog\">
    <interval minimum="00:01:00" />
    <expire />
  </add>
  <add name="tomcat" path="L:\logs\tomcat\">
    <interval minimum="00:01:00" />
    <expire />
  </add>
</storage>
<senders>
  <sender name="server-d" uri="https://server-d.domain.local/caber" />
</senders>
<routes>
  <route from="*" to="server-d" />
</routes>
```
* Declare two storage areas.
  * Limit the rate at which changes are observed, because these services are
    frequently writing new data.
  * Default expiry time of a month will be used.
* Declare our HTTPS destination.
* Route files from all (both) storage areas to the destination.

## Server C
```xml
<storage>
  <add name="some-app" path="E:\app\log\">
    <expire interval="1 week" />
    <location path="perf\" graft="E:\app\perf\" />
  </add>
</storage>
<senders>
  <sender name="server-d" uri="https://server-d.domain.local/caber" />
</senders>
<routes>
  <route from="some-app" to="server-d" />
</routes>
```
* Declare a storage area.
  * Map another directory 'into' it, so it's treated as part of this
    hierarchy.
  * Expire logs after a week instead of the default of a month. No need for
    that clean-up script any more!
* Set up destination and routing.

## Finally, our receiving server D
```xml
<storage>
  <add name="this-site" path="D:\logs\" />
</storage>
<receivers>
  <receiver name="local-services" uri="https://server-d.domain.local/caber" store="this-site" />
</receivers>
<senders>
  <sender name="hq-archive-1" uri="https://hq-archive-1.company.domain/caber" />
  <sender name="hq-archive-2" uri="https://hq-archive-2.company.domain/caber" />
  <sender name="hq-archive-3" uri="https://hq-archive-3.company.domain/caber" />
</senders>
<routes>
  <route from="this-site" to="hq-archive-*" replication="60%" />
</routes>
```
* Set up our storage area.
  * This is not our final destination so we don't configure log expiry.
* Set up a receiving endpoint.
  * Authorisation to post logs here will be requested by each of the services
    when they first start up. This will need to be granted from the console,
    based on the senders' names, storage names, thumbprints and apparent IP
    addresses.
  * Received files go to `D:\logs\<sender's store>\<file path>`, so server B's
    `tomcat` and `postgres` stores map to separate directories on D:.
* Set up three senders, since HQ has three archival servers.
* Route logs from our master store to all three archival servers.
  * Set a replication rate of 60%, requiring that at least 60% of the
    destinations get the data before it will be considered for expiry. We've
    turned off expiry here though so this won't do much.
   