using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Web;

namespace FinanceCalendar
{
    [ApiController]
    [Route("/")]
    public class FinanceCalendarController : ControllerBase
    {
        private readonly Regex userIdsPattern = new Regex(@"^([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(\|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})*$");
        private readonly TokenService _tokenService;
        private readonly FinanceCalendarContext _context;
        private readonly Calendar calendar;

        public FinanceCalendarController(TokenService tokenService, FinanceCalendarContext context, Calendar cal)
        {
            _tokenService = tokenService;
            _context = context;
            calendar = cal;
        }

        [HttpPost]
        [Route("register-user")]
        public IActionResult RegisterUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(
                    new ApiResponse<object>.Builder()
                        .message("Invalid user data.")
                        .Build()
                );
            }

            // Check if a user with the same username already exists
            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                return Conflict(
                    new ApiResponse<object>.Builder()
                        .message("A user with the same username already exists.")
                        .Build()
                );
            }

            // Hash the password using BCrypt
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            var account = new Account { UserId = user.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = _context.Expenses.Where(e => e.UserId == user.Id).ToList();
            user.Account = account;
            var token = _tokenService.GenerateToken(user);

            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(
                new ApiResponse<User>.Builder()
                    .message("User registered successfully.")
                    .user(user)
                    .Build()
            );
        }

        [HttpPost]
        [Route("login-user")]
        public IActionResult LoginUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(
                    new ApiResponse<object>.Builder()
                        .message("Invalid login data")
                        .Build()
                );
            }

            var existingUser = _context.Users.SingleOrDefault(u => u.UserName == user.UserName);
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password))
            {
                return BadRequest(
                    new ApiResponse<object>.Builder()
                        .message("Invalid username or password")
                        .Build()
                );
            }

            var account = new Account { UserId = existingUser.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = _context.Expenses.Where(e => e.UserId == existingUser.Id).ToList();
            existingUser.Account = account;

            var token = _tokenService.GenerateToken(existingUser);
            
            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            });
            return Ok(
                new ApiResponse<User>.Builder()
                    .message("Login successful.")
                    .user(existingUser)
                    .Build()
            );
        }

        [Authorize]
        [HttpGet]
        [Route("get-user")]
        public IActionResult GetUser()
        {  
            var userId = User.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return _Logout();
            }

            var user = _context.Users.SingleOrDefault(u => u.Id == Guid.Parse(userId));
            if (user == null)
            {
                return _Logout();
            }

            var token = Request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return _Logout();
            }

            var account = _tokenService.DecodeToken(token);
            account.Expenses = _context.Expenses.Where(e => e.UserId == user.Id).ToList();
            user.Account = account;

            return Ok(
                new ApiResponse<User>.Builder()
                    .message("User retrieved successfully.")
                    .user(user)
                    .Build()
            );
        }

        private IActionResult _Logout()
        {
            Response.Cookies.Delete("finance-calendar-jwt");
            return Ok(
                new ApiResponse<object>.Builder()
                    .message("Logout successful.")
                    .Build()
            );
        }

        [Authorize]
        [HttpGet]
        [Route("logout")]
        public IActionResult Logout()
        {
            return _Logout();
        }

        [Authorize]
        [HttpGet]
        [Route("get-calendar")]
        public IActionResult GetCalendar()
        {
            User user = _tokenService.GetUser(Request);

            Console.WriteLine("GET Calendar");

            List<List<Day>> cal = calendar.GenerateCalendar(user);

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .message("Calendar retrieved successfully.")
                    .user(user)
                    .data(cal)
                    .Build()
            );
        }

        [Authorize]
        [HttpGet]
        [Route("change-month/{direction}")]
        public IActionResult UpdateMonth(int direction)
        {
            User user = _tokenService.GetUser(Request);
            user.Account.Month += direction;
            if (direction == 0)
            {
                user.Account.Month = DateTime.Now.Month;
                user.Account.Year = DateTime.Now.Year;
            }
            else if (user.Account.Month > 12)
            {
                user.Account.Month = 1;
                user.Account.Year += 1;
            }
            else if (user.Account.Month < 1)
            {
                user.Account.Month = 12;
                user.Account.Year -= 1;
            }
            var token = _tokenService.GenerateToken(user);

            Response.Cookies.Append("finance-calendar-jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            List<List<Day>> cal = calendar.GenerateCalendar(user);

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .message("Month updated successfully.")
                    .user(user)
                    .data(cal)
                    .Build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("add-expense")]
        public IActionResult AddExpense()
        {
            User user = _tokenService.GetUser(Request);

            Expense expense = new() { UserId = user.Id, Id = Guid.NewGuid() };
            _context.Expenses.Add(expense);
            _context.SaveChanges();

            return Ok(
                new ApiResponse<Expense>.Builder()
                    .message("Expense added successfully.")
                    .user(user)
                    .data(expense)
                    .Build()
            );
        }

        [Authorize]
        [HttpDelete]
        [Route("delete-expense/{expenseId}")]
        public IActionResult DeleteExpense(Guid expenseId)
        {
            User user = _tokenService.GetUser(Request);

            var expense = _context.Expenses.FirstOrDefault(e => e.Id == expenseId);
            if (expense == null)
            {
                return NotFound(
                    new ApiResponse<object>.Builder()
                        .message("Expense not found.")
                        .Build()
                );
            }

            _context.Expenses.Remove(expense);
            _context.SaveChanges();

            return Ok(
                new ApiResponse<object>.Builder()
                    .message("Expense deleted successfully.")
                    .Build()
            );
        }

        [Authorize]
        [HttpPut]
        [Route("update-expense")]
        public IActionResult UpdateExpense([FromBody] Expense expense)
        {
            User user = _tokenService.GetUser(Request);

            var existingExpense = _context.Expenses.FirstOrDefault(e => e.Id == expense.Id);
            if (existingExpense == null)
            {
                _context.Expenses.Add(expense);
            }
            else
            {
                var properties = typeof(Expense).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.Name == "Id") continue;

                    var newValue = prop.GetValue(expense);
                    prop.SetValue(existingExpense, newValue);
                }
            }

            _context.SaveChanges();

            return Ok(
                new ApiResponse<Expense>.Builder()
                    .message("Expense successfully updated")
                    .data(expense)
                    .Build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("refresh-calendar")]
        public IActionResult RefreshCalendar()
        {
            User user = _tokenService.GetUser(Request);
            user.Account.Expenses = _context.Expenses.Where(e => e.UserId == user.Id).ToList();
            List<List<Day>> cal = calendar.GenerateEventsFromExpenses(user);
            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .message("Calendar refreshed successfully.")
                    .user(user)
                    .data(cal)
                    .Build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("update-checking-balance")]
        public IActionResult UpdateCheckingBalance([FromBody] double checkingBalance)
        {
            User _user = _tokenService.GetUser(Request);
            var user = _context.Users.Where(u => u.Id == _user.Id).FirstOrDefault();
            if (user == null)
            {
                return _Logout();
            }
            user.CheckingBalance = checkingBalance;
            _context.SaveChanges();

            Console.WriteLine($"update-checking-balance (new - {checkingBalance}) {user.CheckingBalance}");

            calendar.CalculateEventTotals(user);
            List<List<Day>> cal = calendar.GenerateCalendar(user);

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .message("Checking Balance updated successfully.")
                    .user(user)
                    .data(cal)
                    .Build()
            );
        }
    }
}