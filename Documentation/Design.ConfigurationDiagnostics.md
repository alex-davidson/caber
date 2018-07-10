# Design: Configuration Diagnostics

* Up: [Design](Design.md)

Objective: collect and report configuration errors clearly.

* Must make source file and line number available, and this should be as
  accurate as reasonably possible.
* Should avoid generating error cascades, where one failure leads to a dozen
  other irrelevant failures.
* It is not necessary to report all errors in one go, but we should aim to
  reduce the number of fix-run-fix cycles.

## Analysis

There are, broadly speaking, four types of configuration error:
1. Format: invalid file format, schema violations, etc. These render the file
   effectively unreadable, but .NET will generate our errors for us.
2. Syntactic: invalid or incomprehensible values which do not 'spill over'
   into other parts of the configuration and do not prevent the rest of the
   file from being read.
3. Context-free semantic: insane values, which can be read but do not make
   sense. Similar to syntactic errors, these can all be checked in isolation
   without regard for the rest of the configuration.
4. Context-dependent semantic: invalid complex structures, conflicts. These
   can only be validated after multiple pieces of configuration have been
   read. Because of this, they can be prone to generating cascading errors if
   eg. one otherwise-valid structure depends on the creation of another,
   invalid structure.

Categories 1 and 2 are both handled entirely by .NET's own configuration
library. An exception is generally the result. I will assume that these sorts
of glaring error are rare and easily avoided/rectified by the user, and avoid
cluttering the code with lots of checks for these exceptions.

Categories 2 and 3 are somewhat similar and .NET can in fact check 3 as well
in many cases, eg. relative vs. absolute paths. But these types of error are
more likely and sometimes harder to understand, so we should make the effort
to handle category 3 ourselves. We will not use the more advanced built-in
validation capabilities of .NET's configuration library.

Category 4 must be handled by application code, since some errors may not
become apparent until we are building our internal structures. Cascading
failures are a real problem here and we will need to go out of our way to
deal with them. In addition, a category 3 error which prevents the creation
of a structure may cause category 4 breakage when a dependent structure is
rendered invalid by it.

Ultimately it may prove more sensible to just handle configuration as XML
and ignore the System.Configuration namespace, but it will suffice for an
initial implementation.

## General Approach

We will use readers and builders to convert configuration into model.
The reader walks the configuration hierarchy and calls methods on the builder
to construct the model.

Both the reader and the builder record errors as they proceed, and avoid
trying to make invalid state a part of the internal model.

* The reader records category 3 errors and may skip entire subtrees of
  configuration.
* The builder records category 4 errors and is expected to throw if it
  encounters a category 3 error.

To avoid cascades going out of control, the reader should skip the rest of a
subtree if the builder reports an error during its construction.

### Handling Cascades

Errors should be rich structures containing enough information to reconstruct
what went wrong and format a useful explanation. It should also be possible to
infer which errors stem from which errors.

By structuring our configuration file such that dependencies work only down
the tree where possible, we eliminate a lot of cascade situations since an
error up the tree allows us to write off the entire subtree.

Conflicts are still possible across the tree, but these cannot cascade since
an error which eliminates part of the model will only suppress conflicts, not
cause more.

### Reducing Fix Cycles

We assume that category 1 and 2 are rare and easily fixed.

Category 3 can be collected in full, but skipping subtrees to avoid cascades
complicates this. However, we could catch category 3s in the subtree by simply
passing down a stubbed builder class.

Of category 4, we may miss conflicts if a conflicting structure is skipped in
response to another failure. I don't see any way of avoiding this without
either permitting the construction of an invalid model (which makes validation
harder) or tracking a lot of extra state (which would mean considerable bug-
-prone duplication of effort).
