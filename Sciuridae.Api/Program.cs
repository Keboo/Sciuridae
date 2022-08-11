using Azure.Data.Tables;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Sciuridae.Api.Controllers;
using Sciuridae.Api.Data;
using Sciuridae.Api.Providers;
using Sciuridae.Api.Auth;

var builder = WebApplication.CreateBuilder(args);

var creds = new DefaultAzureCredential();
//TODO: Document with local secrets
//TODO: Make KV configurable and optional
builder.Configuration.AddAzureKeyVault(new Uri("https://sciuridae.vault.azure.net/"), creds);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddHmacAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("HMAC", policy =>
    {
        policy.RequireAuthenticatedUser();
    });
});

string aadClientId = builder.Configuration.GetSection("AzureAd").GetValue<string>("ClientId");

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Enabled OAuth security in Swagger
    Dictionary<string, string> scopes = new()
    {
        { $"api://{aadClientId}/access_as_user", "Access application on user behalf" }
    };
    c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
    {
        new OpenApiSecurityScheme {
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "oauth2"
            },
            Scheme = "oauth2",
            Name = "oauth2",
            In = ParameterLocation.Header
        },
        new List<string>()
    }});
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow()
            {
                AuthorizationUrl = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/authorize"),
                TokenUrl = new Uri("https://login.microsoftonline.com/common/common/v2.0/token"),
                Scopes = scopes
            }
        }
    });

});

builder.Services.AddSingleton<ProviderFactory>();
//TODO: Config Uri
builder.Services.AddScoped(x => new TableServiceClient(new Uri("https://sciuridae.table.core.windows.net/"), creds));
builder.Services.AddScoped<AppInformation>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var swaggerSection = builder.Configuration.GetSection("Swagger");
    string clientSecret = swaggerSection.GetValue<string>("ClientSecret");
    string redirectUri = swaggerSection.GetValue<string>("RedirectUri");

    options.EnableDeepLinking();
    options.EnableTryItOutByDefault();
    options.OAuthAppName("Sciuridae");
    options.OAuthClientId(aadClientId);
    options.OAuthClientSecret(clientSecret);
    options.OAuth2RedirectUrl(redirectUri);
    options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
});

//}

using (var dbScope = app.Services.CreateScope())
{
    var tableService = dbScope.ServiceProvider.GetRequiredService<TableServiceClient>();
    await CreateTableAsync(tableService, Release.TableName);
    await CreateTableAsync(tableService, App.TableName);
    
    static async Task CreateTableAsync(TableServiceClient tableService, string tableName)
    {
        var tableClient = tableService.GetTableClient(tableName);
        await tableClient.CreateIfNotExistsAsync();
    }
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
