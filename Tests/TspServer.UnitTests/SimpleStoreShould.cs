using Shouldly;

namespace TspServer.UnitTests;

public class SimpleStoreShould
{
    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public void ReturnCorrectStatistics(int count)
    {
        // Arrange
        using var store = new SimpleStore();
        for (var i = 0; i < count; i++)
            store.Set($"key{i}", new UserProfile { Id = 0, Username = $"Username{i}" });

        // Act
        var tasks = new List<Task>();
        for (byte i = 0; i < count; i++)
        {
            var current = i;
            var currentStore = store;
            var profile = new UserProfile { Id = current, Username = $"Username{i}" };
            tasks.Add(Task.Run(() => currentStore.Get($"key{current}")));
            tasks.Add(Task.Run(() => currentStore.Set($"key{current}", profile), TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() => currentStore.Get($"key{current}")));
        }

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Task.WaitAll([.. tasks], TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        var (setCounter, getCounter, deleteCount) = store.GetStatistics();

        // Assert
        setCounter.ShouldBe(count * 2);
        getCounter.ShouldBe(count * 2);
        deleteCount.ShouldBe(0);
        for (byte i = 0; i < count; i++)
            store.Get($"key{i}")?.Id.ShouldBe(i);
    }
}
