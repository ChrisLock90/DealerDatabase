namespace DealerDatabase.Import.Abstractions;

using System.Collections.Generic;
using DealerDatabase.Import.Importing;
public interface ISourceDataLoader
{
    IReadOnlyList<SourceDealerRecord> LoadAll();
}
