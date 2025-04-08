using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;
using Microsoft.OpenApi.Models;
using Stemma.Redis;
using Microsoft.Extensions.Hosting;
Env.Load();
// Environment.GetEnvironmentVariable("GOOGLE_PRIVATE_KEY")

var builder = WebApplication.CreateBuilder(args);

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "DEVELOPMENT_SECRET_KEY";
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var tokenInCookie = context.HttpContext.Request.Cookies["token"];
                if (!string.IsNullOrEmpty(tokenInCookie))
                {
                    context.Token = tokenInCookie;
                }

                return Task.CompletedTask;
            }
        };
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:3000", // so, this is defect here. make sure to remove before deploy.
                    "https://stemma.vercel.app"
                )
                .AllowCredentials()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


builder.Services.AddHttpClient();
builder.Services.AddStackExchangeRedisCache(options =>
{
    var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "";
    var redisPassword = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? "";
    //Console.WriteLine($"REDIS_HOST: {redisHost}");
    //Console.WriteLine($"REDIS_PASSWORD: {redisPassword}");
    options.Configuration = $"{redisHost},password={redisPassword}";

});

builder.Services.AddScoped<IRedisService, RedisService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token with 'Bearer ' prefix",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowLocalhost");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();