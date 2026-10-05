namespace DealerDatabase.Import.Abstractions;

using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Matching;

public interface IDealerMatcher
{
    MatchOutput Match(IReadOnlyList<SourceDealerRecord> records);
}
