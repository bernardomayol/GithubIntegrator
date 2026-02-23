using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Gh.ControllersApi.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private static readonly Dictionary<string, (byte[] Hash, byte[] Salt, string[] Roles)> _users = Seed();

        private static Dictionary<string, (byte[] Hash, byte[] Salt, string[] Roles)> Seed()
        {
            var dict = new Dictionary<string, (byte[] Hash, byte[] Salt, string[] Roles)> (StringComparer.OrdinalIgnoreCase);
            AddUser(dict, "bernardo", "P@ssw0rd!", new[] { "User" });
            AddUser(dict, "admin", "P@ssw0rd!", new[] {"Admin", "User" });
            return dict;
        }

        private static void AddUser(Dictionary<string, (byte[] Hash, byte[] Salt, string[] roles)> dict, string user, string password, string[] roles)
        {
            // PBKDF2 con 100,000 iteraciones (seguro para demo)
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[16];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(32);

            dict[user] = (hash, salt, roles);
        }

        private static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100_000, HashAlgorithmName.SHA256);
            var computed = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }

        public record LoginRequest(string Username, string Password);

        [HttpPost("token")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Token([FromBody] LoginRequest req, [FromServices] IConfiguration cfg)
        {
            if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
                return Unauthorized(new { error = "Credenciales inválidas." });

            if (!_users.TryGetValue(req.Username, out var entry) || !VerifyPassword(req.Password, entry.Hash, entry.Salt))
                return Unauthorized(new { error = "Usuario o contraseña incorrectos." });

            // Construye claims básicos
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, req.Username),
                new(JwtRegisteredClaimNames.UniqueName, req.Username),
                new("name", req.Username)
            };
            foreach (var r in entry.Roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            // Clave simétrica (>= 32 chars)
            var jwtKey = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            // (Opcional) issuer/audience si los usas
            var issuer = cfg["Jwt:Issuer"];
            var audience = cfg["Jwt:Audience"];

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);
            return Ok(new { access_token = jwt });
        }

    }
}
