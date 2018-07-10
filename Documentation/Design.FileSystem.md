# Design: Storage and the File System

* Up: [Design](Design.md)
* [Filtering](Design.FileSystem.Filters.md)
  
We have two models:
* the OS filesystem, which is a single hierarchy.
  * *We are considering Windows' drive letters to be a layer of a 'master'
    hierarchy.*
* the Caber abstract filesystem, which is a set of named hierarchies.

The latter is mapped onto the former in various non-overlapping ways.

## Concepts

* A Caber abstract hierarchy has a unique **RootName**:
  `RootName={}`
* A **LocalRoot** is an OS directory represented in some opaque manner:
  `LocalRoot={}`
* A **NamedRoot** is a LocalRoot declared as the root of a Caber abstract
  hierarchy:
  `NamedRoot={RootName,LocalRoot}`
* A **RelativePath** is a normalised path which is relative to some
  externally-defined root:
  `RelativePath={}`
* A **QualifiedPath** is an OS file represented as a LocalRoot and a
  RelativePath:
  `QualifiedPath={LocalRoot,RelativePath}`
* An **AbstractPath** is a file's location in an abstract hierarchy:
  `AbstractPath={RootName,RelativePath}`
* A **Graft** is an OS directory declared as existing at a certain
  QualifiedPath, ie. a RelativePath within a LocalRoot:
  `Graft={QualifiedPath,LocalRoot}`

## Operations

Local roots are not permitted to overlap at present, but this may be relaxed
in future.

Given an absolute OS path and a list of all local roots, we can find a 'best'
containing LocalRoot by finding the one with the longest path which is a
prefix of the absolute path, then create a QualifiedPath relative to that.

Given a QualifiedPath, we can work our way up the Graft hierarchy to find its
NamedRoot. We can then concatenate the RelativePaths of the Graft chain to
create the AbstractPath.

Given an AbstractPath, we can look up the appropriate NamedRoot, then look for
child grafts shortest-first to determine the 'best' QualifiedPath.

Thus is a bidirectional mapping between OS and abstract hierarchies achieved.

## Path Equality

While Windows is our current target platform, we cannot rely on
case-insensitivity since modern Windows operating systems transparently
support case-sensitive *directories*, which presents difficulties if we want
to handle this properly across different machines.

It makes sense to preserve case, so let's start by ensuring that we always
work with canonical, correctly-cased paths.

Let's say server A has a case-sensitive hierarchy which it is shipping to
server B, which already has those directories configured as case-insensitive
and may already have subdirectories with the 'wrong' casing.

What is the correct behaviour?

1. B could set the necessary attributes on its directories. But this is
   hideously dangerous since other things may be using those directories, or
   other (case-insensitive) siblings of A might be shipping stuff there.
2. B could ignore the case of A's requests, potentially losing information, or
   even causing hash conflicts due to two distinct differently-cased files
   being received.
3. B could refuse to have anything to do with A until their case-handling
   matches. This would highlight a potential misconfiguration to the user, and
   require that it either be rectified or explicitly overridden.

Option 3 is really the *only* correct behaviour.

Now, at what granularity do we handle this? A's OS hierarchy may bear no
resemblance at all to B's. All the two have in common is the abstract
hierarchy. This means that we really have to establish casing rules for the
entirety of an abstract hierarchy, ie. NamedRoot and all its Grafts.

## Validation

For each named root, the Graft model must be an arborescence (tree). No cycles
are permitted in the graph, and each graft is permitted to have only a single
parent.

Ideally we want each failure to be associated with a location in the
configuration file. This means either recording extra metadata with each
component of the model, or taking the former approach and raising errors as
they're discovered.

We want to report *all* errors at once, as far as is reasonably possible. But
one error may cause a cascade of other errors, all resolved by fixing the
first. 

There are two approaches for validating the configuration:
* During construction of the model. Location information can be supplied as
  errors are encountered, but an error may cause cascading failures as parts
  of the model are rendered unavailable.
* Analysis of the completed model. Location information needs to be tracked
  as the model is built, but cascading errors from eg. a missing node should
  be less common.

The former is considerably easier to implement and test, and the cascading
error problem may be mitigated by postprocessing the set of errors, or by
having the building process skip children of failed configuration elements.

The former also eliminates any possibility of cycles in the graph, since a
graft's child can never be added twice and a graft's parent must always exist.

### Rules

LocalRoots must be unique and can never overlap:

    when adding a LocalRoot a:
		for each existing LocalRoot b:
			if a === b, error.
			if ospath(a) == ospath(b), error.
		    if ospath(a) contains ospath(b), error.
			if ospath(b) contains ospath(a), error.

Duplicate graft points and grafted roots are not permitted, and every grafted
root must use the same casing rules as its parent:

	when adding a Graft a:
		if casing(a.QualifiedPath.Root) != casing(a.ChildRoot), error.
		for each existing Graft b:
			if a.QualifiedPath == b.QualifiedPath, error.
		for each existing LocalRoot c:
			if a.ChildRoot == c, error.
			if ospath(a.ChildRoot) == ospath(c), error.
