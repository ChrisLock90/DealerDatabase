namespace DealerDatabase.Import.Abstractions;

using System.Collections.Generic;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;

public interface IConsolidationService
{
    List<Dealer> BuildDealers(MatchOutput output);
}
