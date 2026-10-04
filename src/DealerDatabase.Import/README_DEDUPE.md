Tests and logging added for director deduplication

What I added:
- xUnit test project file ConsolidationServiceTests.cs verifying directors are deduplicated and merged.
- ConsolidationService.Dedupe.cs: DeduplicateDirectors(IList<Dealer>) method that groups directors by (Name, Role), merges contact fields preferring non-empty values, and marks merged items in the Other field for quick logging.
- Program.cs updated to call DeduplicateDirectors before saving and to log merged director entries via the existing logger.

Notes:
- I used the DealerDirector.Other string as a lightweight place to attach merge notes to avoid changing the EF model in this quick iteration. For production, add a dedicated metadata property or logging pipeline.
- Next steps: expand merge rules, add more unit tests for edge cases, and add integration test hitting an in-memory or temp SQLite instance.
