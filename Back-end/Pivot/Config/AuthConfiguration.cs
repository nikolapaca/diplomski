using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Pivot.Config
{
    public class AuthConfiguration
    {
        private static void ConfigureAuthentication(IServiceCollection services)
        {
            var key = Environment.GetEnvironmentVariable("JWT_KEY") ?? "pivot_secret_key";
            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "pivot";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "pivot-front.com";

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                            {
                                context.Response.Headers.Add("AuthenticationTokens-Expired", "true");
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}
