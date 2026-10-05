# 01-prerequisites: Verify .NET 8 readiness

Confirm the .NET 8 SDK and repository configuration are compatible with the selected target framework before touching project files. This includes checking any version pinning or solution-level settings that could force a different runtime, and making sure the workspace is ready for a single-pass upgrade.

## Research Notes
- The assessment shows 8 SDK-style projects: 3 application/library projects already target net8.0, and 5 test projects currently target net10.0.
- `validate_dotnet_sdk_installation` confirmed the net8.0 SDK is installed and available in the environment.
- A repository search for `global.json`, `Directory.Build.props`, and `Directory.Packages.props` returned no matches, so there is no repo-level framework override visible from the workspace root.
- No code changes are needed for this prerequisite task; the work is purely validation and setup for the retargeting step.

**Done when**: .NET 8 is confirmed as the active target for the solution, and no environment or solution-level settings block the retargeting work.
