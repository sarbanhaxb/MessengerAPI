using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MessengerAPI.Models;
using MessengerAPI.Services;
using MessengerAPI.Hubs;

var builder = WebApplication.CreateBuilder(args);

#region Настройка сервисов
// 1. Добавление controllers
builder.Services.AddControllers();

// 2. Настраивание DatabaseSettings из appsettings.json
// Биндинг секции "DatabaseSettings" на класс DatabaseSettings
builder.Services.Configure<DatabaseSettings>(
    builder.Configuration.GetSection("DatabaseSettings"));

// 3. Регистрация сервисаов
// Singleton - один экземпляр на всё приложение для работы с БД
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<IMessageService, MessageService>();
builder.Services.AddSingleton<IChatService, ChatService>();

// 4. Настройка JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"];

builder.Services.AddAuthentication(options =>
{
    // По умолчанию используем JWT Bearer
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,  // Проверять издателя токена
        ValidateAudience = true,  // Проверять аудиторию
        ValidateLifetime = true,  // Проверять срок действия
        ValidateIssuerSigningKey = true,  // Проверять подпись
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!))
    };

    // Настройка для SignalR (чтобы JWT работал с WebSocket)
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Если это SignalR запрос, берём токен из query string
            var accessToken = context.Request.Query["access_token"];

            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});


// 5. Добавляем SignalR
builder.Services.AddSignalR();

// 6. Настраиваем CORS (чтобы фронтенд мог обращаться к API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")  // URL фронтенда
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 7. Добавляем Swagger (документация API)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Настройка JWT в Swagger (чтобы можно было тестировать защищённые endpoints)
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
#endregion

// Сборка приложения
var app = builder.Build();

#region MIDDLEWARE PIPELINE


// 1. Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 2. HTTPS редирект
app.UseHttpsRedirection();

// 3. CORS 
app.UseCors("AllowReactApp");

// 4. Authentication
app.UseAuthentication();

// 5. Authorization
app.UseAuthorization();

// 6. Controllers
app.MapControllers();

// 7. SignalR Hub
app.MapHub<ChatHub>("/chathub"); // Клиенты подключаются к ws://localhost:5000/chathub
#endregion

// Запуск приложения
app.Run();