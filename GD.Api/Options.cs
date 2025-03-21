using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace GD.Api
{
    public class Options
    {
        public class AuthOptions
        {
            public const string ISSUER = "GD Auth"; // издатель токена
            public const string AUDIENCE = "GD PWA"; // потребитель токена
            const string KEY = "mysupersecret_secretsecretsecretkey!645";   // ключ для шифрации
            public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
        }
    }
}
