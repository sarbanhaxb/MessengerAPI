using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MessengerAPI.Services;

namespace MessengerAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Все методы требуют авторизации
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        #region Получить всех пользователей
        // GET /api/users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                // Извлечение из токена пользователя
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Неавторизован"
                    });
                }

                // Получение всех пользователей, кроме текущего
                var users = await _userService.GetAllUsersAsync(currentUserId);

                return Ok(new
                {
                    success = true,
                    users = users.Select(u => new
                    {
                        id = u.Id,
                        name = u.Name,
                        email = u.Email,
                        avatar = u.Avatar,
                        status = u.Status,
                        position = u.Position
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка получения пользователей",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Получить профиль пользователя
        // GET /api/users/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(string userId)
        {
            try
            {
                var user = await _userService.GetByIdAsync(userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Пользователь не найден"
                    });
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
                    message = "Ошибка получения профиля",
                    error = ex.Message
                });
            }
        }
        #endregion

        #region Обновить статус
        // PUT /api/users/status
        [HttpPut("status")]
        public async Task<IActionResult> UpdateStatus([FromBody] StatusRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Проверка корректности статуса
                if (request.Status != "online" && request.Status != "offline")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Статус должен быть 'online' или 'offline'"
                    });
                }

                // Обновление статуса в БД
                await _userService.UpdateStatusAsync(userId, request.Status);

                return Ok(new
                {
                    success = true,
                    message = "Статус обновлен"
                });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Ошибка обновления статуса",
                    error = ex.Message
                });
            }
        }
        #endregion
    }
}
// DTO для обновления статуса
public class StatusRequest
{
    public string Status { get; set; } = null!;
}