using DealerDatabase.Import.Importing;
using DealerDatabase.Import.Matching;

namespace DealerDatabase.Import.Abstractions;

public interface IDealerMatcher
{
    MatchOutput Match(IReadOnlyList<SourceDealerRecord> records);
}
