# 02-upgrade-solution: Retarget all projects to .NET 8

Update the solution’s projects so every project targets net8.0. The main application and library projects already match that target; the work here is to bring the remaining test projects into alignment so the solution is standardized on a single framework version.

Because the assessment found no package incompatibilities or API incidents, this should be a straightforward TFM alignment task. Preserve the existing SDK-style project structure and keep the change focused on target framework consistency across the solution.

## Scope Inventory
- **Projects affected**: `DealerDatabase.Data.Tests/DealerDatabase.Data.UnitTests.csproj`, `DealerDatabase.Import.Tests/DealerDatabase.Import.UnitTests.csproj`, `DealerDatabase.Data.IntegrationTests.cs/DealerDatabase.Data.IntegrationTests.csproj`, `DealerDatabase.Import.IntegrationTests/DealerDatabase.Import.IntegrationTests.csproj`, and `Dealer.Import.FeatureTests/Dealer.Import.FeatureTests.csproj`.
- **Distinct concern**: single TFM alignment pass — replace `net10.0` with `net8.0` in the solution-level test projects; no package or code API changes were flagged by assessment.
- **Change signals**: all affected projects are SDK-style and currently only differ from the rest of the solution by their target framework. The application/library projects already target net8.0 and have no compatibility issues.
- **Notes**: a separate `src/DealerDatabase.Import.Tests/DealerDatabase.Import.Tests.csproj` exists in the repository and already targets net8.0, but it is outside the assessed solution scope.

## Research Notes
- The repo search confirmed the five solution test projects currently target net10.0 and the three production projects already target net8.0.
- No `global.json`, `Directory.Build.props`, or `Directory.Packages.props` files were found at the repository root search scope, so there is no visible repo-level property override that would block a project-file TFM change.
- The task is a direct property edit across the five test projects; no package updates or code fixes are expected from the assessment.

## Validation Notes
- As of the current workspace state, all solution projects already target `net8.0`; the remaining work is workflow reconciliation for the stale in-progress task.
- Full solution build and test validation completed successfully after confirming the standardized target framework baseline.

**Done when**: Every project in the solution targets net8.0 and the solution files reflect a consistent framework baseline.
