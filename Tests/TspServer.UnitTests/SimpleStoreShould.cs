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
            store.Set($"key{i}", [0]);

        // Act
        var tasks = new List<Task>();
        for (byte i = 0; i < count; i++)
        {
            var current = i;
            tasks.Add(Task.Run(() => store.Get($"key{current}")));
            tasks.Add(Task.Run(() => store.Set($"key{current}", [current]), TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() => store.Get($"key{current}")));
        }

#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Task.WaitAll([.. tasks], TestContext.Current.CancellationToken);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        var (setCounter, getCounter, _) = store.GetStatistics();

        // Assert
        setCounter.ShouldBe(count * 2);
        getCounter.ShouldBe(count * 2);
        for (byte i = 0; i < count; i++)
            store.Get($"key{i}").FirstOrDefault().ShouldBe(i);
    }
}
