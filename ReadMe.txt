Developer Test
==============

This solution implements the supplied word-frequency assessment for both the
synchronous and asynchronous APIs.

Requirements
------------

- Visual Studio or Build Tools with the .NET Framework 4.8 Developer Pack
- NUnit 3 test dependencies from the repository's packages directory

Projects
--------

- DeveloperTest contains DeveloperTestImplementation and
  DeveloperTestImplementationAsync.
- DeveloperTestInterfaces contains the reader, output, and implementation
  contracts.
- DeveloperTestSupport contains the supplied character readers.
- DeveloperTestFrameworkUnitTests contains the NUnit test suite.

Behavior
--------

Question one reads a single character stream and outputs case-insensitive word
frequencies ordered by descending count and then alphabetically.

Question two processes all supplied readers in parallel. It writes a combined
snapshot every ten seconds and writes the final combined counts when all readers
reach the end of their streams.

The asynchronous implementation provides the same behavior, passes cancellation
tokens to asynchronous reads, and propagates cancellation to its caller.

Words consist of letters and may contain a single internal hyphen. Punctuation,
whitespace, repeated hyphens, and trailing hyphens terminate a word. A final word
is counted even when the stream ends without a delimiter.

Build
-----

From a Visual Studio Developer PowerShell prompt:

    dotnet msbuild DeveloperTest.sln /t:Rebuild /p:Configuration=Release /warnaserror

Test
----

After the Release build, run the tests with Visual Studio Test Explorer or:

    vstest.console.exe DeveloperTestFrameworkUnitTests\bin\Release\DeveloperTestFramework.dll /TestAdapterPath:packages\NUnit3TestAdapter.3.2.0

The slow-reader tests intentionally take several minutes because they verify the
ten-second progress reporting and parallel reader processing.
