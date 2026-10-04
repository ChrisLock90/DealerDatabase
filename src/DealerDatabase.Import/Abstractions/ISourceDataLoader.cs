using System.Collections.Generic;
using DealerDatabase.Import.Importing;

namespace DealerDatabase.Import.Abstractions;

public interface ISourceDataLoader
{
    IReadOnlyList<SourceDealerRecord> LoadAll();
}
