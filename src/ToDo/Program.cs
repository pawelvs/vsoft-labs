// <snippet_all>
using NSwag.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Kubernetes;
using Azure.Messaging.ServiceBus;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;


var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

var connString = builder.Configuration.GetConnectionString("AZURE_SQL_CONNECTIONSTRING");

// Dodaj po builder.Services.AddEndpointsApiExplorer();
var keyVaultUri = new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/");
var secretClient = new SecretClient(keyVaultUri, new DefaultAzureCredential());
var serviceBusConnection = secretClient.GetSecret("ServiceBusConnection").Value.Value;

builder.Services.AddSingleton(new ServiceBusClient(serviceBusConnection));
builder.Services.AddSingleton<ServiceBusService>();


builder.Services.AddDbContext<TodoDb>(opt => opt.UseSqlServer(connString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(config =>
{
    config.DocumentName = "TodoAPI";
    config.Title = "TodoAPI v1";
    config.Version = "v1";
});

builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.EnableAdaptiveSampling = false; // Wyłączenie adaptacyjnego samplingu
    options.EnableDependencyTrackingTelemetryModule = true; // Distributed tracing
    options.EnablePerformanceCounterCollectionModule = true; // Metryki wydajności
    options.EnableAppServicesHeartbeatTelemetryModule = true; // Heartbeat
    options.EnableDebugLogger = true; // Debugowanie w konsoli (dla deweloperów)
});

// Dodanie Kubernetes Enricher
builder.Services.AddApplicationInsightsKubernetesEnricher();

var app = builder.Build();


app.UseOpenApi();
app.UseSwaggerUi(config =>
{
    config.DocumentTitle = "TodoAPI";
    config.Path = "/swagger";
    config.DocumentPath = "/swagger/{documentName}/swagger.json";
    config.DocExpansion = "list";
});

// <snippet_group>
RouteGroupBuilder todoItems = app.MapGroup("/todoitems");


app.Services.GetService<TodoDb>()!.Database.Migrate();

todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id}", GetTodo);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id}", UpdateTodo);
todoItems.MapDelete("/{id}", DeleteTodo);
// </snippet_group>

app.Run();

// <snippet_handlers>
// <snippet_getalltodos>
static async Task<IResult> GetAllTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Select(x => new TodoItemDTO(x)).ToArrayAsync());
}
// </snippet_getalltodos>

static async Task<IResult> GetCompleteTodos(TodoDb db)
{
    return TypedResults.Ok(await db.Todos.Where(t => t.IsComplete).Select(x => new TodoItemDTO(x)).ToListAsync());
}

static async Task<IResult> GetTodo(int id, TodoDb db)
{
    return await db.Todos.FindAsync(id)
        is Todo todo
            ? TypedResults.Ok(new TodoItemDTO(todo))
            : TypedResults.NotFound();
}

static async Task<IResult> CreateTodo(TodoItemDTO todoItemDTO, TodoDb db, ServiceBusService serviceBus)
{
    var todoItem = new Todo
    {
        IsComplete = todoItemDTO.IsComplete,
        Name = todoItemDTO.Name
    };

    db.Todos.Add(todoItem);
    await db.SaveChangesAsync();

    var dto = new TodoItemDTO(todoItem);
    await serviceBus.SendMessageAsync(new TodoEvent("TodoCreated", dto));

    return TypedResults.Created($"/todoitems/{todoItem.Id}", dto);
}

static async Task<IResult> UpdateTodo(int id, TodoItemDTO todoItemDTO, TodoDb db, ServiceBusService serviceBus)
{
    var todo = await db.Todos.FindAsync(id);

    if (todo is null) return TypedResults.NotFound();

    todo.Name = todoItemDTO.Name;
    todo.IsComplete = todoItemDTO.IsComplete;

    await db.SaveChangesAsync();

    await serviceBus.SendMessageAsync(new TodoEvent(nameof(UpdateTodo), todoItemDTO));
    return TypedResults.NoContent();
}

static async Task<IResult> DeleteTodo(int id, TodoDb db, ServiceBusService service)
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        var dto = new TodoItemDTO(todo);
        await service.SendMessageAsync(new TodoEvent(nameof(DeleteTodo), dto));
        return TypedResults.NoContent();
    }

    return TypedResults.NotFound();
}
// <snippet_handlers>
// </snippet_all>
