using Backend.Data;
using Backend.Options;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Backend.Models;

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
    });

builder.Services.AddControllers()
    . AddJsonOptions(json => json.JsonSerializerOptions.Converters.
        Add( new System.Text.Json.Serialization.JsonStringEnumConverter()));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddDbContext<AppDbContext> ( 
    option => option.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);
builder.WebHost.UseUrls("http://0.0.0.0:4000");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await TranslationsSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();
