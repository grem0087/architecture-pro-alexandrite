using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "OrderService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddConsoleExporter()
        .AddOtlpExporter(options =>
        {
            options.Endpoint = new Uri("http://localhost:4317"); 
            options.Protocol = OtlpExportProtocol.Grpc; 
        }));
builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/order", async (IHttpClientFactory httpClientFactory, ILogger<Program> logger,
    HttpContext context) =>
{
    logger.LogInformation("Создание заказа");

    var orderId = Guid.NewGuid();
    var orderDate = DateTime.UtcNow;

    var calculationServiceUrl = "http://localhost:5001/calculate";

    try
    {
        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync($"{calculationServiceUrl}?orderId={orderId}");

        if (response.IsSuccessStatusCode)
        {
            var calculationResult = await response.Content.ReadAsStringAsync();

            return Results.Ok(new
            {
                OrderId = orderId,
                Status = "Created",
                CreatedAt = orderDate,
                Calculation = calculationResult,
                Message = "Заказ создан"
            });
        }

        return Results.Problem("Ошибка при расчёте заказа");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ошибка при создании заказа");
        return Results.Problem("Внутренняя ошибка сервера");
    }
});

app.Run("http://localhost:5000");
