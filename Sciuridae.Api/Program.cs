using Azure.Data.Tables;
using Azure.Identity;
using Sciuridae.Api.Controllers;
using Sciuridae.Api.Data;
using Sciuridae.Api.Providers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ProviderFactory>();
//TODO: Config Uri
builder.Services.AddScoped(x => new TableServiceClient(new Uri("https://sciuridae.table.core.windows.net/"), new DefaultAzureCredential()));
builder.Services.AddScoped<AppInformation>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var dbScope = app.Services.CreateScope())
{
    var tableService = dbScope.ServiceProvider.GetRequiredService<TableServiceClient>();
    var tableClient = tableService.GetTableClient(Release.TableName);
    await tableClient.CreateIfNotExistsAsync();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
