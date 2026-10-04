using System.Collections.Generic;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;

namespace DealerDatabase.Import.Abstractions;

public interface IConsolidationService
{
    List<Dealer> BuildDealers(MatchOutput output);
}
