namespace DealerDatabase.Tests.Data;

using System.IO;
using DealerDatabase.Data;
using NUnit.Framework;

[TestFixture]
public class SolutionPathsTests
{
    [Test]
    public void SolutionRoot_ReturnsExistingDirectoryContainingSolutionFile()
    {
        // Act
        var root = SolutionPaths.SolutionRoot;

        // Assert
        Assert.That(Directory.Exists(root), Is.True);
        Assert.That(File.Exists(Path.Combine(root, "DealerDatabase.sln")), Is.True);
    }

    [Test]
    public void DataDirectory_ReturnsExpectedPathUnderSolutionRoot()
    {
        // Act
        var dataDir = SolutionPaths.DataDirectory;

        // Assert
        Assert.That(dataDir, Is.EqualTo(Path.Combine(SolutionPaths.SolutionRoot, "data")));
    }

    [Test]
    public void DatabaseFile_ReturnsExpectedPathUnderSolutionRoot()
    {
        // Act
        var dbFile = SolutionPaths.DatabaseFile;

        // Assert
        Assert.That(dbFile, Is.EqualTo(Path.Combine(SolutionPaths.SolutionRoot, "dealers.db")));
    }
}