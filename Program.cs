using OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Trace;
using Honeycomb.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);
var honeycombOptions = builder.Configuration.GetHoneycombOptions();

// Setup OpenTelemetry Tracing
builder.Services.AddOpenTelemetryTracing(otelBuilder =>
    otelBuilder
        .AddHoneycomb(honeycombOptions)
        .AddAutoInstrumentations()
        .AddProcessor<BaggageSpanProcessor>()
);

// Register Tracer so it can be injected into other components (eg Controllers)
builder.Services.AddSingleton(TracerProvider.Default.GetTracer(honeycombOptions.ServiceName));

builder.Services.AddDbContext<BeeDb>(opt => opt.UseInMemoryDatabase("bees"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();




var app = builder.Build();


app.UseSwagger();

app.MapGet("/", (Tracer tracer) =>
{
    Baggage.Current.SetBaggage("app.username", "martin");
    using var span = tracer.StartActiveSpan("app.manual-span");
    span.SetAttribute("app.manual-span.message", "Adding custom spans is also super easy!");
});

app.MapGet("/bees", async (BeeDb db) =>
    await db.Bees.ToListAsync());

app.MapGet("/bees/{id}", async (int id, BeeDb db) =>
    await db.Bees.FindAsync(id)
        is Bee bee
            ? Results.Ok(bee)
            : Results.NotFound());

app.MapPost("/bees", async (Bee bee, BeeDb db) =>
{
    db.Bees.Add(bee);
    await db.SaveChangesAsync();

    return Results.Created($"/bees/{bee.Id}", bee);
});

app.MapPut("/bees/{id}", async (int id, Bee inputBee, BeeDb db) =>
{
    var bee = await db.Bees.FindAsync(id);

    if (bee is null) return Results.NotFound();

    bee.Name = inputBee.Name;

    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapDelete("/bees/{id}", async (int id, BeeDb db) =>
{
    if (await db.Bees.FindAsync(id) is Bee bee)
    {
        db.Bees.Remove(bee);
        await db.SaveChangesAsync();
        return Results.Ok(bee);
    }

    return Results.NotFound();
});

app.UseSwaggerUI();

await app.RunAsync();
