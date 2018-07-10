# Design: File System, Filtering

* Up: [Storage and File System](Design.FileSystem.md)

We do not necessarily want to include every file under a given location. For
example, if a service automatically compresses old logs, we may want to retain
these on the filesystem but not ship them anywhere.

Our storage locations are configured as hierarchies of hierarchies. Once an OS
path has been converted to a QualifiedPath, it is in a form which we can
easily deal with. Associating filters with LocalRoots makes sense from a
configuration perspective, and filtering against relative path allows filters
to be consistently defined across different platforms.

## Example Configuration

```xml
<storage>
  <add name="root" path="D:\logs\">
    <filters>
      <match extension=".zip" rule="exclude" />
      <match extension=".gz" rule="exclude" />
    </filters>
    <location path="json\">
      <filters>
        <match rule="exclude" />
        <match glob="**/*anonymised*" rule="include" />
      </filters>
    </location>
  </add>
</storage>
```

**Filter syntax:**
* Regexes are probably the ultimate solution for matching paths, but are
  usually overkill. They will be offered as an option, and internally we will
  compile other syntaxes to regexen.
* Globbing is simple, traditional and fairly powerful, although syntax varies
  between applications so designing and clearly documenting a sane dialect
  will be necessary.
* A lot of use cases are covered merely by filtering on file extensions.
* Absent filter syntax indicates a catch-all.

**Filter evaluation:**
* The default behaviour is to include everything, although named roots will
  exclude `.caber` directories which are reserved for internal use.
* Filters are read from least specific to most specific. That is, you define
  your general rules first, and then any exceptions.
* Filters are applied from child to parent, ie. any paths included by a given
  location may be excluded by its parent's filters.

Given the above, an empty `<filters />` element can be read as:
```xml
<filters>
  <match rule="include" />
  <match glob="**\.caber\**" rule="exclude" />
</filters>
```

Because evaluation applies the last matching rule, either of these defaults
can be easily overridden.

Note that the glob here uses Windows path separators rather than forward
slashes. We should tolerate platform-specific path separators in globs,
although doing so properly in regexen is hard so those will require forward
slashes.

## Regex Syntax

Regexen will be used as provided. If they do not explicitly specify ^ and $ to
force whole-string matching, they are permitted to match only part of the
path.

Regex path matching is an advanced feature and we do not expect it to be used
often.

## Glob Syntax

Globs will be compiled to regexen when the configuration is loaded. This can
be done relatively easily and using a consistent matcher implementation makes
debugging simple.

Since we are compiling to regex, we can easily support a very wide range of
glob wildcards. The following wildcards are fairly standard and all have the
caveat that they never match path separators:

* `*`: Matches any number of characters, including none.
* `?`: Matches any single character.
* `[abc]`: Matches any single character given in the bracket.
* `[a-z]`: Matches any single character from the range given in the bracket.
  Note that this is locale-dependent!
* `[!abc]`: Matches any single character which is not given in the bracket.
* `[!a-z]`: Matches any single character which is not from the range given in
  the bracket. Again, this is locale-dependent!

Since it's fairly common to want to match multiple leading directories, we
define an additional wildcard:

* `**`: Matches any number of characters, including path separators.
  If used between path separators or the start/end of the pattern, this
  wildcard will 'absorb' one separator, so `**/.caber` will match `.caber` as
  well as `subdir/.caber`.

## Extension Syntax

This is the simplest type of matcher and the least likely to cause confusion.

File extensions will have a leading period added if it is not specified.
