using System.IO;
using EventProcessing;

public class EventProcessorTests
{
    private EventProcessor eventProcessor;

    public EventProcessorTests()
    {
        eventProcessor = new EventProcessor();
    }

    [Fact]
    public async Task ProcessOrderEventAsync__FirstPaymentOrderSecond__ShouldBePaid()
    {
        var id = "1a-23";
        var product = "Laptop";
        var total = 9999.9m;
        var currency = "CZK";

        await eventProcessor.ProcessOrderEventAsync(id, product, total, currency);

        var paid = 9999.9m;

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        await eventProcessor.ProcessPaymentEventAsync(id, paid);

        var output = stringWriter.ToString();
        Assert.Contains($"Order {id} for {product} has been fully paid.", output);

    }

    [Fact]
    public async Task ProcessOrderEventAsync__FirstOrderPaymentSecond__ShouldBePaid()
    {
        var id = "2a-23";
        var product = "Laptop";
        var total = 9999.9m;
        var currency = "CZK";
        var paid = 9999.9m;

        await eventProcessor.ProcessPaymentEventAsync(id, paid);

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        await eventProcessor.ProcessOrderEventAsync(id, product, total, currency);

        var output = stringWriter.ToString();
        Assert.Contains($"Order {id} for {product} has been fully paid.", output);
    }

    [Fact]
    public async Task ProcessOrderEventAsync__PaymentInTwoWavesInBetween__ShouldBePaid()
    {
        var id = "3a-23";
        var product = "Laptop";
        var total = 9999.9m;
        var currency = "CZK";
        var paidFirst = 9000m;
        var paidSecond = 999.9m;

        await eventProcessor.ProcessPaymentEventAsync(id, paidFirst);

        await eventProcessor.ProcessOrderEventAsync(id, product, total, currency);

        var stringWriter = new StringWriter();
        Console.SetOut(stringWriter);

        await eventProcessor.ProcessPaymentEventAsync(id, paidSecond);

        var output = stringWriter.ToString();
        Assert.Contains($"Order {id} for {product} has been fully paid.", output);
    }
}