using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Security.Claims;

namespace FinanceCalendar
{
    public class TokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user, Account account)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("UserName", user.UserName),
                new Claim("CheckingBalance", user.CheckingBalance.ToString()),
                new Claim("Month", account.Month.ToString()),
                new Claim("Year", account.Year.ToString()),
                new Claim("UserId", user.Id.ToString())
            };

            foreach (var expense in account.Expenses)
            {
                claims.Add(new Claim("Expense", $"{expense.Name}:{expense.Amount}"));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key")));
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
                    return new Expense { Name = parts[0], Amount = decimal.Parse(parts[1]) };
                }).ToList()
            };

            return account;
        }
    }
}