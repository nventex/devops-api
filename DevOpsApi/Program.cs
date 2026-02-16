using DevOpsApi.Authentication;
using DevOpsApi.Common.Infrastructure.DevOps;
using DevOpsApi.Common.Settings;
using DevOpsApi.WorkItemDependency;
using DevOpsApi.WorkItemDependency.Api;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<GetWorkItemDependencyHandler>();
builder.Services.AddScoped<GetWorkItemsDependencyHandler>();
builder.Services.AddScoped<GetWorkItemsHandler>();
builder.Services.AddScoped<DevOpsClient>();
builder.Services.AddScoped<AuthenticationHandler>();
builder.Services.AddLazyCache();

builder.Services.AddOptions<DevOpsSettings>().Configure(options => builder.Configuration.GetSection("DevOpsSettings").Bind(options));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "wwwroot")),
    RequestPath = "/ado",
    DefaultContentType = "text/html"
});
app.MapStaticAssets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapWorkItemDependencyApi();
app.MapWorkItemsDependencyApi();
app.MapCopilotWorkItemsDependencyApi();
app.MapWorkItemsApi();
app.MapAuthentication();

app.Run();