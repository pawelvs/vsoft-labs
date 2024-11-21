using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;


var builder = Host.CreateApplicationBuilder(args);


// Application Insights
// builder.Services.AddApplicationInsightsTelemetry(options =>
// {
//     options.EnableAdaptiveSampling = false; // Wyłączenie adaptacyjnego samplingu
//     options.EnableDependencyTrackingTelemetryModule = true; // Distributed tracing
//     options.EnablePerformanceCounterCollectionModule = true; // Metryki wydajności
//     options.EnableAppServicesHeartbeatTelemetryModule = true; // Heartbeat
//     options.EnableDebugLogger = true; // Debugowanie w konsoli (dla deweloperów)
// });


builder.Services.AddApplicationInsightsTelemetryWorkerService();
builder.Services.AddApplicationInsightsKubernetesEnricher();



// Key Vault configuration
var keyVaultUrl = $"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/";
var credential = new DefaultAzureCredential();
var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

// Storage configuration
var blobServiceClient = new BlobServiceClient(
    new Uri($"https://{builder.Configuration["StorageAccountName"]}.blob.core.windows.net"),
    credential);
builder.Services.AddSingleton(blobServiceClient);

// Service Bus configuration
var serviceBusConnection = secretClient.GetSecret("ServiceBusReader").Value.Value;
var serviceBusClient = new ServiceBusClient(serviceBusConnection);
builder.Services.AddSingleton(serviceBusClient);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();