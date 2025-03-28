using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Web;
using FinanceCalendar.Services;

namespace FinanceCalendar
{
    [ApiController]
    [Route("/")]
    public class FinanceCalendarController : ControllerBase
    {
        private readonly Regex userIdsPattern = new Regex(@"^([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(\|[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})*$");
        private readonly Security _security;
        private readonly Calendar _calendar;

        public FinanceCalendarController(Security security, Calendar calendar)
        {
            _security = security;
            _calendar = calendar;
        }

        [HttpPost]
        [Route("register-user")]
        public IActionResult RegisterUser([FromBody] User user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName) || string.IsNullOrEmpty(user.Password))
            {
                return BadRequest(
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message("Invalid user data.")
                        .build()
                );
            }

            ServiceResponse<string> registration = _security.RegisterUser(user);

            if (!registration.Success)
            {
                return Conflict(
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(registration.Message)
                        .build()
                );
            }

            Response.Cookies.Append("finance-calendar-jwt", registration.Data, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict
            });

            return Ok(
                new ApiResponse<User>.Builder()
                    .success(true)
                    .message(registration.Message)
                    .user(registration.User)
                    .build()
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
                        .success(false)
                        .message("Invalid login data")
                        .build()
                );
            }

            ServiceResponse<string> login = _security.LoginUser(user);

            if (!login.Success)
            {
                return Conflict(
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(login.Message)
                        .build()
                );
            }
            
            Response.Cookies.Append("finance-calendar-jwt", login.Data, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Lax
            });
            return Ok(
                new ApiResponse<User>.Builder()
                    .success(true)
                    .message("Login successful.")
                    .user(login.User)
                    .build()
            );
        }

        [Authorize]
        [HttpGet]
        [Route("get-user")]
        public IActionResult GetUser()
        {  
            ServiceResponse<object> userRes = _security.GetUser(Request);

            if (!userRes.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(userRes.Message)
                        .build()
                );
            }

            return Ok(
                new ApiResponse<User>.Builder()
                    .success(true)
                    .message("User retrieved successfully.")
                    .user(userRes.User)
                    .build()
            );
        }

        private IActionResult _Logout()
        {
            Response.Cookies.Delete("finance-calendar-jwt");
            return Ok(
                new ApiResponse<object>.Builder()
                    .success(true)
                    .message("Logout successful.")
                    .build()
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
        public async Task<IActionResult> GetCalendar()
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);

            if (!userRes.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(userRes.Message)
                        .build()
                );
            }

            User user = userRes.User;

            List<List<Day>> cal = await _calendar.GenerateCalendar(user);

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .message("Calendar retrieved successfully.")
                    .user(user)
                    .data(cal)
                    .build()
            );
        }

        [Authorize]
        [HttpGet]
        [Route("change-month/{direction}")]
        public async Task<IActionResult> UpdateMonth(int direction)
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);

            if (!userRes.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(userRes.Message)
                        .build()
                );
            }

            User user = userRes.User;

            user = _calendar.ChangeMonth(user, direction);
            string token = Request.Cookies["finance-calendar-jwt"];

            ServiceResponse<string> tokenUpdate = _security.UpdateToken(token, user.Account);

            if (!tokenUpdate.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(tokenUpdate.Message)
                        .build()
                );
            }

            Response.Cookies.Append("finance-calendar-jwt", tokenUpdate.Data, new CookieOptions
            {
                HttpOnly = true,
                Secure = false,
                SameSite = SameSiteMode.Strict
            });

            List<List<Day>> cal = await _calendar.GenerateCalendar(user);

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .message("Month updated successfully.")
                    .user(user)
                    .data(cal)
                    .build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("add-expense")]
        public async Task<IActionResult> AddExpense()
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);

            Expense expense = await _calendar.AddExpense(userRes.User);

            userRes = _security.GetUser(Request);

            return Ok(
                new ApiResponse<Expense>.Builder()
                    .success(true)
                    .message("Expense added successfully.")
                    .user(userRes.User)
                    .data(expense)
                    .build()
            );
        }

        [Authorize]
        [HttpDelete]
        [Route("delete-expense/{expenseId}")]
        public async Task<IActionResult> DeleteExpense(Guid expenseId)
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);

            ServiceResponse<object> deleteExpense = await _calendar.DeleteExpense(userRes.User, expenseId);

            if (!deleteExpense.Success)
            {
                return NotFound(
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(deleteExpense.Message)
                        .build()
                );
            }

            userRes = _security.GetUser(Request);

            return Ok(
                new ApiResponse<object>.Builder()
                    .success(true)
                    .user(userRes.User)
                    .message("Expense deleted successfully.")
                    .build()
            );
        }

        [Authorize]
        [HttpPut]
        [Route("update-expense")]
        public async Task<IActionResult> UpdateExpense([FromBody] Expense expense)
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);

            ServiceResponse<object> update = await _calendar.UpdateExpense(userRes.User, expense);

            if (!update.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(update.Message)
                        .build()
                );
            }

            return Ok(
                new ApiResponse<Expense>.Builder()
                    .success(true)
                    .message("Expense successfully updated")
                    .data(expense)
                    .build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("refresh-calendar")]
        public async Task<IActionResult> RefreshCalendar()
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);
            
            ServiceResponse<List<List<Day>>> cal = await _calendar.RefreshCalendar(userRes.User);

            if (!cal.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(cal.Message)
                        .build()
                );
            }

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .message("Calendar refreshed successfully.")
                    .user(userRes.User)
                    .data(cal.Data)
                    .build()
            );
        }

        [Authorize]
        [HttpPost]
        [Route("update-checking-balance")]
        public async Task<IActionResult> UpdateCheckingBalance([FromBody] double checkingBalance)
        {
            ServiceResponse<object> userRes = _security.GetUser(Request);
            
            ServiceResponse<List<List<Day>>> newCal = await _calendar.UpdateCheckingBalance(userRes.User, checkingBalance);

            if (!newCal.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .message(newCal.Message)
                        .build()
                );
            }

            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .message("Checking Balance updated successfully.")
                    .user(newCal.User)
                    .data(newCal.Data)
                    .build()
            );
        }

        [Authorize]
        [HttpPut]
        [Route("save-event")]
        public async Task<IActionResult> SaveThisEvent([FromBody] Event ev, [FromQuery] bool all = false)
        {
            var userRes = _security.GetUser(Request);
            User user = userRes.User;
            if (ev.UserId != user.Id)
            {
                return BadRequest(
                    new ApiResponse<Event>.Builder()
                        .success(false)
                        .data(ev)
                        .message("Invalid event data.")
                        .build()
                ); 
            }
            ServiceResponse<Event> savedEvent = await _calendar.SaveEvent(user, ev, all);
            if (!savedEvent.Success)
            {
                return StatusCode(500,
                    new ApiResponse<object>.Builder()
                        .success(false)
                        .data(ev)
                        .message(savedEvent.Message)
                        .build()
                ); 
            }
            var cal = await _calendar.GenerateCalendar(user);
            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .data(cal)
                    .build()
            );
        }

        [Authorize]
        [HttpDelete]
        [Route("delete-event")]
        public async Task<IActionResult> DeleteEvent([FromQuery] Guid id, [FromQuery] Guid recurrenceId)
        {
            var userRes = _security.GetUser(Request);
            User user = userRes.User;
            ServiceResponse<bool> deletion = await _calendar.DeleteEvent(user, id, recurrenceId);
            if (!deletion.Success)
            {
                return StatusCode(500,
                    new ApiResponse<bool>.Builder()
                        .success(false)
                        .message(deletion.Message)
                        .build()
                ); 
            }
            var cal = await _calendar.GenerateCalendar(user);
            return Ok(
                new ApiResponse<List<List<Day>>>.Builder()
                    .success(true)
                    .data(cal)
                    .build()
            );
        }
    }
}