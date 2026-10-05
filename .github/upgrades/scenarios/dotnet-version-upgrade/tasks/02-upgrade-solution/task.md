# 02-upgrade-solution: Retarget all projects to .NET 8

Update the solution’s projects so every project targets net8.0. The main application and library projects already match that target; the work here is to bring the remaining test projects into alignment so the solution is standardized on a single framework version.

Because the assessment found no package incompatibilities or API incidents, this should be a straightforward TFM alignment task. Preserve the existing SDK-style project structure and keep the change focused on target framework consistency across the solution.

**Done when**: Every project in the solution targets net8.0 and the solution files reflect a consistent framework baseline.
