using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MessengerAPI.Models;
using MessengerAPI.DTOs;
using MessengerAPI.Services;

namespace MessengerAPI.Controllers
{
    /// <summary>
    /// [Route] - базовый путь для всех методов в этом контроллере
    /// Все методы будут начинаться с /api/auth/...
    /// </summary>

    [Route("api/[controller]")]
    [ApiController] // Автоматическая валидация моделей и обработка ошибок
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, IConfiguration configuration)
        {
            _userService = userService;
            _configuration = configuration; // Для доступа к appsettings.json
        }

        #region Регистрация
        // POST /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // [FromBody] - данные берутся из тела запроса (JSON)
            // ASP.NET Core автоматически проверил валиацию (Required, Email, MinLength)
            // Если что-то не так, то этот метода не вызывается

            try
            {
                // Проверка существования пользователя с таким email
                var existingUser = await _userService.GetByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    // Возвращаем ошибку 400 (Bad Request)
                    return BadRequest(new
                    {
                        success = false,
                        message = "Пользователь с таким email уже существует"
                    });
                }

                // Хеширование пароля
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Создание User
                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email.ToLower(),
                    Password = hashedPassword,
                    Status = "offline"
                };

                // Сохранение в БД
                await _userService.CreateAsync(user);

                // ВРЕМЕННЫЙ КОД ДЛЯ ОТЛАДКИ
                Console.WriteLine($"Secret: {_configuration["JwtSettings:Secret"]}");
                Console.WriteLine($"Issuer: {_configuration["JwtSettings:Issuer"]}");
                Console.WriteLine($"Audience: {_configuration["JwtSettings:Audience"]}");

                // Генерация JWT токена
                var token = GenerateJwtToket(user);

                // Возвращаем успешный ответ 201 (Created)
                return CreatedAtAction(nameof(Register), new
                {
                    success = true,
                    message = "Пользователь создан",
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                    }
                });
            }
            catch (Exception ex)
            {
                // Если произошла ошибка, возвращаем 500
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка регистрации",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Вход
        // POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                // Поиск пользователя по email
                var user = await _userService.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    // Возвращаем 401
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Неправильный email или пароль"
                    });
                }

                // Проверка пароля
                var isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
                if (!isPasswordValid)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Неправильный пароль"
                    });
                }

                // Генерация токена
                var token = GenerateJwtToket(user);

                return Ok(new
                {
                    success = true,
                    message = "Успешный вход",
                    token,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка входа",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Получить текущего пользователя
        // GET /api/auth/me
        // [Authorize] - требует JWT токен в заголовке Authorization
        [HttpGet("me")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Достает userId из JWT токена

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { success = false, message = "Неавторизован" });
                }

                // Загрузка пользователя из БД
                var user = await _userService.GetByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "Пользователь не найден" });
                }

                return Ok(new
                {
                    success = true,
                    user = new
                    {
                        id = user.Id,
                        name = user.Name,
                        email = user.Email,
                        avatar = user.Avatar,
                        status = user.Status,
                        position = user.Position
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка получения данных",
                    error = ex.Message
                });
            }
        }
        #endregion


        // Генерация JWT токена
        private string GenerateJwtToket(User user)
        {
            // Claims - данные, хранящиеся в токене
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
            };

            // Берем секретный ключ из appsettings.json
            var secret = _configuration["JwtSettings:Secret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!));

            // Создание подписи токена
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Время действия токена
            var expirationDays = int.Parse(_configuration["JwtSettings:ExpirationDays"]!);

            // Создание токена
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(expirationDays),
                signingCredentials: creds
            );


            // Конвертируем токен в строку
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}