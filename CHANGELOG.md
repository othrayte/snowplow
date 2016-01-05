# Change Log
All notable changes to this project will be documented in this file.

## [Unreleased]
### Added
 - Support for localisable strings.
 - Compliance with VS built in C# code analysis ("Microsoft All Rules"")

## [1.1.4.0] - 2015-12-22
### Added
 - More info link pointing to the github project.

## [1.1.3.0] - 2015-12-22
### Changed
 - pdb files removed from VSIX when building in release.

## [1.1.2.0] - 2015-12-22
### Added
 - LICENSE, README and CHANGELOG.

## [1.1.1.0] - 2015-12-21
### Fixed
 - Fixed bug where tests writing to stdout would prevent SnowPlow from understanding the xml output.

## [1.1.0.0] - 2015-12-21
### Added
 - Support for debugging unit tests

## [1.0.9.0] - 2015-12-17
### Added
 - Recognise file and line number information when returned when discovering unit tests. This information is only provided by igloo when the relevant extensions are present.

## [1.0.8.0] - 2015-12-16
### Added
 - Option to group tests in the unit tests explorer by the root level describe.

## [1.0.7.0] - 2015-12-16
### Changed
 - '::' and "_" are now stripped from the names of unit tests when constructing their display name (hover over the name to see the original name).

## [1.0.5.0] - 2015-12-15
### Changed
 - Move the error message of failed unit tests to the message field in the unit test explorer and extract the file and line number to the stack trace section.

## [1.0.4.0] - 2015-12-15
### Changed
 - Pass --list option to unit test .exe files, this is ignored by igloo if unsupported.

## [1.0.3.1] - 2015-12-15
### Fixed
 - VSIX installer now actually installs a snowplow.dll that actually contains SnowPlow.
