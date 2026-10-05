# .NET Version Upgrade Plan

## Overview

**Target**: Standardize the DealerDatabase solution on .NET 8.
**Scope**: 8 SDK-style projects. The application and library projects already target net8.0; the test projects currently target net10.0 and need to align with the rest of the solution.

### Selected Strategy
**All-At-Once** — All projects upgraded simultaneously in a single operation.
**Rationale**: 8 projects, all on modern .NET, clear dependency structure, and no compatibility issues or package blockers in the assessment.

## Tasks

### 01-prerequisites: Verify .NET 8 readiness

Confirm the .NET 8 SDK and repository configuration are compatible with the selected target framework before touching project files. This includes checking any version pinning or solution-level settings that could force a different runtime, and making sure the workspace is ready for a single-pass upgrade.

The assessment showed no incompatible packages and no framework-specific blockers, so this task is primarily about validating the environment and upgrade target rather than resolving code issues.

**Done when**: .NET 8 is confirmed as the active target for the solution, and no environment or solution-level settings block the retargeting work.

---

### 02-upgrade-solution: Retarget all projects to .NET 8

Update the solution’s projects so every project targets net8.0. The main application and library projects already match that target; the work here is to bring the remaining test projects into alignment so the solution is standardized on a single framework version.

Because the assessment found no package incompatibilities or API incidents, this should be a straightforward TFM alignment task. Preserve the existing SDK-style project structure and keep the change focused on target framework consistency across the solution.

**Done when**: Every project in the solution targets net8.0 and the solution files reflect a consistent framework baseline.

---

### 03-validation: Build and test the standardized solution

Validate the upgraded solution end-to-end after the framework alignment is complete. This includes a clean build of the solution and the relevant test suites to confirm the net8.0 standardization did not introduce regressions.

Treat any build or test failure as upgrade fallout to resolve before completing the scenario.

**Done when**: The solution builds successfully and the relevant tests pass on .NET 8.
