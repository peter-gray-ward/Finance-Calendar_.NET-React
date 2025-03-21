using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;

namespace FinanceCalendar
{
    [ApiController]
    [Route("/")]
    public class FinanceCalendarController : ControllerBase
    {
        private readonly Regex userIdsPattern = new Regex(@"^([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(\|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})*$");
        private readonly TokenService _tokenService;
        private readonly FinanceCalendarContext _context;

        public FinanceCalendarController(TokenService tokenService, FinanceCalendarContext context)
        {
            _tokenService = tokenService;
            _context = context;
        }

        [HttpPost]
        [Route("register-user")]
        public IActionResult RegisterUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new ApiResponse<User>("Invalid user data.", "Invalid user data.", null));
            }

            // Check if a user with the same username already exists
            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                return Conflict(new ApiResponse<User>("A user with the same username already exists.", "Conflict", null));
            }

            // Hash the password using BCrypt
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            var account = new Account { UserId = user.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            var token = _tokenService.GenerateToken(user, account);

            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new ApiResponse<object>("User registered successfully.", null, new { user, token }));
        }

        [HttpPost]
        [Route("login-user")]
        public IActionResult LoginUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(new ApiResponse<User>("Invalid login data.", "Invalid login data.", null));
            }

            var existingUser = _context.Users.SingleOrDefault(u => u.UserName == user.UserName);
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password))
            {
                return Unauthorized(new ApiResponse<User>("Invalid username or password.", "Unauthorized", null));
            }

            var account = new Account { UserId = existingUser.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            var token = _tokenService.GenerateToken(existingUser, account);

            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            });
            return Ok(new ApiResponse<object>("Login successful.", null, new { user = existingUser, token }));
        }

        [Authorize]
        [HttpGet]
        [Route("get-user")]
        public IActionResult GetUser()
        {
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
            }   
            var userId = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Console.WriteLine($"Extracted userId from claims: {userId}");

            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("User not authenticated.");
                return Unauthorized(new ApiResponse<User>("User not authenticated.", "Unauthorized", null));
            }

            var user = _context.Users.SingleOrDefault(u => u.Id == Guid.Parse(userId));
            if (user == null)
            {
                Console.WriteLine("User not found.");
                return NotFound(new ApiResponse<User>("User not found.", "Not Found", null));
            }

            var token = Request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ApiResponse<User>("Token not found.", "Unauthorized", null));
            }

            var account = _tokenService.DecodeToken(token);
            user.Account = account;

            return Ok(new ApiResponse<User>("User retrieved successfully.", null, user));
        }

        [Authorize]
        [HttpGet]
        [Route("get-calendar")]
        public IActionResult GetCalendar(Calendar calendar)
        {
            var token = Request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new ApiResponse<User>("Token not found.", "Unauthorized", null));
            }

            var account = _tokenService.DecodeToken(token);
            var user = _context.Users.SingleOrDefault(u => u.Id == account.UserId);
            if (user == null)
            {
                return Unauthorized(new ApiResponse<User>("Token not found.", "Unauthorized", null));
            }

            DateTime currentMonth = new DateTime(account.Year, account.Month, 1);
            DateTime previousMonth = currentMonth.AddMonths(-1);
            DateTime nextMonth = currentMonth.AddMonths(2);

            var events = _context.Events
                .Where(e => e.UserId == user.Id && e.Date >= previousMonth && e.Date < nextMonth)
                .ToList();
            
            var weeks = calendar.GetWeeks(user, account.Month, account.Year, new List<Event>());

            return Ok(new ApiResponse<object>("Calendar retrieved successfully.", null, weeks));
        }

        [Authorize]
        [HttpPost]
        [Route("update-month")]
        public IActionResult UpdateMonth([FromBody] int month)
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<User>("User not authenticated.", "Unauthorized", null));
            }

            var user = _context.Users.SingleOrDefault(u => u.Id == Guid.Parse(userId));
            if (user == null)
            {
                return NotFound(new ApiResponse<User>("User not found.", "Not Found", null));
            }

            var account = new Account { UserId = user.Id, Month = month, Year = DateTime.Now.Year };
            var token = _tokenService.GenerateToken(user, account);

            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new ApiResponse<object>("Month updated successfully.", null, new { user, token }));
        }
    }
}