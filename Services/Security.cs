using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using FinanceCalendar;

namespace FinanceCalendar.Services
{
    public class Security
    {
        private readonly IConfiguration _configuration;
        private readonly FinanceCalendarContext _context;

        public Security(IConfiguration configuration, FinanceCalendarContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task<ServiceResponse<string>> RegisterUser(User user)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == user.UserName))
            {
                return new ServiceResponse<string>.Builder()
                    .success(false)
                    .message("A user with the same username already exists.")
                    .build();
            }

            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var account = new Account { UserId = user.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = await _context.Expenses.Where(e => e.UserId == user.Id).ToListAsync();
            user.Account = account;

            var token = GenerateToken(user);

            return new ServiceResponse<string>.Builder()
                .success(true)
                .message("User registered successfully.")
                .user(user)
                .data(token)
                .build();
        }

        public async Task<ServiceResponse<string>> LoginUser(User user)
        {
            var existingUser = await _context.Users.SingleOrDefaultAsync(u => u.UserName == user.UserName);
            if (existingUser == null || !BCrypt.Net.BCrypt.Verify(user.Password, existingUser.Password))
            {
                return new ServiceResponse<string>.Builder()
                    .success(false)
                    .message("Invalid username or password")
                    .build();
            }

            var account = new Account { UserId = existingUser.Id, Month = DateTime.Now.Month, Year = DateTime.Now.Year };
            account.Expenses = await _context.Expenses.Where(e => e.UserId == existingUser.Id).ToListAsync();
            account.Debts = await _context.Debts.Where(d => d.UserId == existingUser.Id).ToListAsync();
            existingUser.Account = account;

            var token = GenerateToken(existingUser);

            return new ServiceResponse<string>.Builder()
                .success(true)
                .message("User logged-in successfully.")
                .user(existingUser)
                .data(token)
                .build();
        }

        public async Task<ServiceResponse<object>> GetUser(HttpRequest request)
        {
            var token = request.Cookies["finance-calendar-jwt"];
            if (string.IsNullOrEmpty(token))
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message("Token is missing or invalid.")
                    .build();
            }

            var account = DecodeToken(token);

            var user = await _context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Id == account.UserId);

            if (user == null)
            {
                return new ServiceResponse<object>.Builder()
                    .success(false)
                    .message("User not found.")
                    .build();
            }

            var expenses = await _context.Expenses
                .Where(e => e.UserId == user.Id)
                .AsNoTracking()
                .ToListAsync();

            var debts = await _context.Debts
                .Where(d => d.UserId == user.Id)
                .AsNoTracking()
                .ToListAsync();

            user.Account = new Account
            {
                UserId = user.Id,
                Month = account.Month,
                Year = account.Year,
                Expenses = expenses,
                Debts = debts
            };

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
                new Claim("Month", user.Account.Month.ToString()),
                new Claim("Year", user.Account.Year.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("TimeZone", user.TimeZone)
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

        public async Task<ServiceResponse<string>> UpdateToken(string token, Account account)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key"));

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateLifetime = false // We may be updating expired tokens too
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var claims = jwtToken.Claims.ToList();

                claims.RemoveAll(c => c.Type == "Month" || c.Type == "Year");
                claims.Add(new Claim("Month", account.Month.ToString()));
                claims.Add(new Claim("Year", account.Year.ToString()));

                var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

                var newToken = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddMinutes(30),
                    signingCredentials: creds);

                return new ServiceResponse<string>.Builder()
                    .success(true)
                    .data(tokenHandler.WriteToken(newToken))
                    .build();
            }
            catch (Exception ex)
            {
                return new ServiceResponse<string>.Builder()
                    .success(false)
                    .message(ex.Message)
                    .build();
            }
        }

        private Account DecodeToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            return new Account
            {
                UserId = Guid.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value ?? Guid.Empty.ToString()),
                Month = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Month")?.Value ?? "0"),
                Year = int.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == "Year")?.Value ?? "0")
            };
        }
    }
}