using SuperUI.Base.Utilities;
using Xunit;

namespace SuperUI.Tests.Base;

public class SgIdGeneratorTests
{
    [Fact]
    public void NewId_returns_non_empty_string()
    {
        var id = SgIdGenerator.NewId();
        Assert.False(string.IsNullOrEmpty(id));
    }

    [Fact]
    public void NewId_returns_unique_ids()
    {
        var ids = new HashSet<string>();
        for (var i = 0; i < 1000; i++)
            ids.Add(SgIdGenerator.NewId());
        Assert.Equal(1000, ids.Count);
    }

    [Fact]
    public void NewId_returns_with_prefix()
    {
        var id = SgIdGenerator.NewId("button");
        Assert.StartsWith("button-", id);
    }

    [Fact]
    public void StableIdFor_returns_same_id_for_same_owner()
    {
        var owner = new object();
        var a = SgIdGenerator.StableIdFor(owner, "test");
        var b = SgIdGenerator.StableIdFor(owner, "test");
        Assert.Equal(a, b);
    }

    [Fact]
    public void StableIdFor_returns_different_ids_for_different_owners()
    {
        var ownerA = new object();
        var ownerB = new object();
        var a = SgIdGenerator.StableIdFor(ownerA, "test");
        var b = SgIdGenerator.StableIdFor(ownerB, "test");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void StableIdForGeneric_includes_type_name_in_prefix()
    {
        var owner = new SgIdGeneratorTests();
        var id = SgIdGenerator.StableIdFor<SgIdGeneratorTests>(owner);
        // Type is SgIdGeneratorTests — strip "Sg" prefix to get "idgeneratortests".
        Assert.StartsWith("sg-idgeneratortests-", id);
    }
}
