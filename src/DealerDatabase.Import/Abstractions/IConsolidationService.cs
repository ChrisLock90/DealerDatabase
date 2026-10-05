namespace DealerDatabase.Import.Abstractions;

using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Matching;
using System.Collections.Generic;

public interface IConsolidationService
{
    List<Dealer> BuildDealers(MatchOutput output);
}
