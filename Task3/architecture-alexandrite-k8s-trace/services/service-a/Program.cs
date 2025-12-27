using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "CalculationService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317");
            options.Protocol = OtlpExportProtocol.Grpc; 
        }));


var app = builder.Build();


app.MapGet("/calculate", (string orderId, ILogger<Program> logger) =>
{
    logger.LogInformation("Начат расчёт для заказа {OrderId}", orderId);

    var random = new Random();
    var calculationTime = random.Next(50, 500);

    Thread.Sleep(calculationTime);

    var totalAmount = random.Next(100, 10000);
    var tax = totalAmount * 0.2m;
    var discount = totalAmount * 0.1m;
    var finalAmount = totalAmount + tax - discount;

    logger.LogInformation("Расчёт завершён для заказа {OrderId}", orderId);

    return new
    {
        OrderId = orderId,
        TotalAmount = totalAmount,
        Tax = tax,
        Discount = discount,
        FinalAmount = finalAmount,
        CalculationTimeMs = calculationTime,
        CalculatedAt = DateTime.UtcNow
    };
});

app.Run("http://localhost:5001");
