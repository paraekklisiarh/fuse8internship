using InternalApi.Entities;

namespace InternalApi.Tests;

public class InternalQueueTests
{

    [Fact]
    public void InternalCurrencyConversionQueue_ThreadSafety()
    {
        // Arrange
        IInternalQueue<CurrencyConversionTask> sut = new InternalCurrencyConversionQueue();
        var values = Enum.GetValues(typeof(CurrencyType));
        List<Task> tasks;
        
        // 1000 задач в очередь
        tasks = new List<Task>();
        foreach (var e in Enumerable.Range(1, 100))
        {
            var randomIndex = new Random().Next(0, values.Length);
            var conversionTask = new CurrencyConversionTask
            {
                Status = CurrencyConversionStatus.Created,
                NewBaseCurrency = (CurrencyType)values.GetValue(randomIndex)!,
                StartTime = DateTimeOffset.UtcNow,
            };
            tasks.Add(new Task(() => sut.Enqueue(conversionTask)));
        }

        // 1000 задач из очереди
        tasks.AddRange(Enumerable.Range(1, 100).Select(_ => new Task(() => sut.Dequeue())));

        // Act
        var exception = Record.Exception(() =>
        {
            tasks.ForEach(t => t.Start());
            Task.WaitAll(tasks.ToArray());
        });
        
        // Assert
        Assert.Null(exception);
    }
}