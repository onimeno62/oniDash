using oniDash.Core.Domain;
using Xunit;

namespace oniDash.Core.Tests;

public sealed class TestEntity : Entity
{
    public TestEntity()
    {
    }

    public TestEntity(Guid id)
    {
        Id = id;
    }
}

public sealed class OtherTestEntity : Entity
{
    public OtherTestEntity()
    {
    }

    public OtherTestEntity(Guid id)
    {
        Id = id;
    }
}

public sealed class EntityTests
{
    [Fact]
    public void New_entity_gets_a_non_empty_identity()
    {
        var entity = new TestEntity();

        Assert.NotEqual(Guid.Empty, entity.Id);
    }

    [Fact]
    public void Entities_with_the_same_identity_and_type_are_equal()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new TestEntity(id);

        Assert.True(first == second);
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Entities_with_different_identities_are_not_equal()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        Assert.True(first != second);
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Entities_of_different_concrete_types_are_not_equal_even_with_same_id()
    {
        var id = Guid.NewGuid();
        var first = new TestEntity(id);
        var second = new OtherTestEntity(id);

        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Entity_is_never_equal_to_null()
    {
        var entity = new TestEntity(Guid.NewGuid());

        Assert.True(entity != null);
        Assert.False(entity.Equals(null));
    }
}
