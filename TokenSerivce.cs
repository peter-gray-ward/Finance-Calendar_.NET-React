using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;

namespace FinanceCalendar
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;
        private readonly FinanceCalendarContext _context;

        public TokenService(IConfiguration configuration, FinanceCalendarContext _context)
        {
            _configuration = configuration;
            this._context = _context;
        }

        public User GetUser(HttpRequest request)
        {
            var token = request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("Token is missing or invalid.");
            }

            Account account = DecodeToken(token);
            var user = _context.Users.SingleOrDefault(u => u.Id == account.UserId);
            if (user == null)
            {
                throw new UnauthorizedAccessException("User not found.");
            }

            user.Account = account;
            return user;
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

            var account = new Account
            {
                UserId = Guid.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? Guid.Empty.ToString()),
                Month = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Month")?.Value ?? "0"),
                Year = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Year")?.Value ?? "0"),
                Expenses = jwtToken.Claims.Where(c => c.Type == "Expense").Select(c =>
                {
                    var parts = c.Value.Split(':');
                    return new Expense { Name = parts[0], Amount = double.Parse(parts[1]) };
                }).ToList()
            };

            return account;
        }
    }
}