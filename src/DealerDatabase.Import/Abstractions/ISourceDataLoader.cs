namespace DealerDatabase.Import.Abstractions;

using DealerDatabase.Import.Importing;
using System.Collections.Generic;
public interface ISourceDataLoader
{
    IReadOnlyList<SourceDealerRecord> LoadAll();
}
