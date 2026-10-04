using System.Collections.Generic;
using System.Linq;
using DealerDatabase.Data.Entities;
using DealerDatabase.Import.Importing;
using Xunit;

namespace DealerDatabase.Import.Tests;

public class ConsolidationServiceTests
{
    [Fact]
    public void Deduplicates_Directors_By_Name_And_Role_And_Merges_Occupation_And_Nationality()
    {
        var svc = new ConsolidationService();

        var dealer = new Dealer
        {
            Id = 1,
            Name = "Example Dealer",
            Directors = new List<DealerDirector>
            {
                new DealerDirector { Name = "Alice Smith", Role = "Director", Occupation = "", Nationality = "British" },
                new DealerDirector { Name = "Alice Smith ", Role = "Director", Occupation = "Manager", Nationality = "" },
                new DealerDirector { Name = "Bob Jones", Role = "Director", Occupation = "Sales", Nationality = "" }
            }
        };

        var dealers = new List<Dealer> { dealer };

        var result = svc.DeduplicateDirectors(dealers);

        var d = result.Single();
        Assert.Equal(2, d.Directors.Count);

        var alice = d.Directors.Single(x => x.Name.Trim() == "Alice Smith");
        Assert.Equal("Manager", alice.Occupation);
        Assert.Equal("British", alice.Nationality);
        Assert.False(string.IsNullOrWhiteSpace(alice.MergeNote));
    }

    [Fact]
    public void Deduplicates_Case_And_Whitespace_Insensitive()
    {
        var svc = new ConsolidationService();

        var dealer = new Dealer
        {
            Id = 2,
            Name = "Case Dealer",
            Directors = new List<DealerDirector>
            {
                new DealerDirector { Name = "Bob", Role = "Director" },
                new DealerDirector { Name = " bob ", Role = "director" },
                new DealerDirector { Name = "BOB", Role = "Director" }
            }
        };

        var dealers = new List<Dealer> { dealer };
        var result = svc.DeduplicateDirectors(dealers);
        var d = result.Single();
        Assert.Single(d.Directors);
    }
}
