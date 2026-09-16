using Backend.Data;
using Backend.Options;
using Backend.Services;
using StackExchange.Redis;
using Backend.Hubs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddOpenApi();
builder.Services.AddCors( options =>
{
    options.AddPolicy("frontend", policy => 
        policy.WithOrigins("http://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
    );
});
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<GoogleOptions>(builder.Configuration.GetSection("Google"));
builder.Services.Configure<GithubOptions>(builder.Configuration.GetSection("Github"));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!)
);

var jwt = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(optins =>
    {
        optins.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer =            true,
            ValidIssuer =               jwt["Issuer"],
            ValidateAudience =          true,
            ValidAudience =             jwt["Audience"],
            ValidateLifetime =          true,
            ClockSkew =                 TimeSpan.Zero,
            ValidateIssuerSigningKey =  true,
            IssuerSigningKey =          new SymmetricSecurityKey( Encoding.UTF8.GetBytes(jwt["key"]!)),
        };
        optins.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddControllers()
    . AddJsonOptions(json => json.JsonSerializerOptions.Converters.
        Add( new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<FriendsService>();
builder.Services.AddScoped<OAuthService>();

builder.Services.AddSingleton<GameStateService>();
builder.Services.AddSingleton<OnlineStateService>();
builder.Services.AddSingleton<MatchmakingService>();
builder.Services.AddSingleton<RoomCountdownService>();
builder.Services.AddSingleton<GameLoopService>();

builder.Services.AddHttpClient();
builder.Services.AddSignalR();
builder.Services.AddDbContext<AppDbContext> ( 
    option => option.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);
builder.WebHost.UseUrls("http://0.0.0.0:4000");

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await TranslationsSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment()) {
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<GameHub>("/hubs/game");

app.Run();
