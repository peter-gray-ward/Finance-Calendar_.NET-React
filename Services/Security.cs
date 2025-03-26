using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using FinanceCalendar;

namespace FinanceCalendar.Services
{
    public class Security
    {
        private readonly IConfiguration _configuration;
        private readonly FinanceCalendarContext _context;

        public Security(IConfiguration configuration, FinanceCalendarContext _context)
        {
            _configuration = configuration;
            this._context = _context;
        }

        public ServiceResponse<string> RegisterUser(User user)
        {
            if (_context.Users.Any(u => u.UserName == user.UserName))
            {
                return new ServiceResponse<string>.Builder()
                    .success(false)
                    .message("A user with the same username already exists.")
                    .build();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            var account = new Account { UserId = user.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = _context.Expenses.Where(e => e.UserId == user.Id).ToList();
            user.Account = account;

            var token = GenerateToken(user);

            return new ServiceResponse<string>.Builder()
                .success(true)
                .message("User registered successfully.")
                .user(user)
                .data(token)
                .build();
        }

        public ServiceResponse<string> LoginUser(User user)
        {
            var existingUser = _context.Users.SingleOrDefault(u => u.UserName == user.UserName);
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password))
            {
                return new ServiceResponse<string>.Builder()
                    .success(false)
                    .message("Invalid username or password")
                    .build();
            }

            var account = new Account { UserId = existingUser.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = _context.Expenses.Where(e => e.UserId == existingUser.Id).ToList();
            existingUser.Account = account;

            var token = GenerateToken(existingUser);

            return new ServiceResponse<string>.Builder()
                .success(true)
                .message("User logged-in successfully.")
                .user(existingUser)
                .data(token)
                .build();
        }

        public ServiceResponse<object> GetUser(HttpRequest request)
        {
            var token = request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message("Token is missing or invalid.")
                    .build();
            }

            Account account = DecodeToken(token);
            var user = _context.Users.SingleOrDefault(u => u.Id == account.UserId);
            if (user == null)
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message("User not found")
                    .build();
            }

            user.Account = account;

            return new ServiceResponse<object>.Builder()
                    .success(true)
                    .user(user)
                    .build();
        }

        public string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserName", user.UserName),
                new Claim("CheckingBalance", user.CheckingBalance.ToString()),
                new Claim("Month", user.Account.Month.ToString()),
                new Claim("Year", user.Account.Year.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("TimeZone", "Eastern Standard Time") // 👈 hardcoded timezone
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        public Account DecodeToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var userId = Guid.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? Guid.Empty.ToString());

            var account = new Account
            {
                UserId = userId,
                Month = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Month")?.Value ?? "0"),
                Year = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Year")?.Value ?? "0"),
                Expenses = _context.Expenses.Where(e => e.UserId == userId).ToList()
            };

            return account;
        }
    }
}