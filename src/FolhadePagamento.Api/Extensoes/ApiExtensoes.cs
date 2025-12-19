using System.Text;
using FolhadePagamento.Api.Configuracoes;
using FolhadePagamento.Api.Servicos;
using FolhadePagamento.Infra;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace FolhadePagamento.Api.Extensoes;

/// <summary>
/// Extensões para configuração de serviços da API.
/// </summary>
public static class ApiExtensoes
{
    /// <summary>
    /// Adiciona autenticação JWT.
    /// </summary>
    public static IServiceCollection AdicionarAutenticacaoJwt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configurar JwtSettings
        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        services.Configure<JwtSettings>(jwtSection);

        var jwtSettings = jwtSection.Get<JwtSettings>() 
            ?? throw new InvalidOperationException("JwtSettings não configurado.");

        // Registrar serviço JWT
        services.AddScoped<IJwtService, JwtService>();

        // Configurar autenticação
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.ChaveSecreta)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Emissor,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audiencia,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Adiciona versionamento de API.
    /// </summary>
    public static IServiceCollection AdicionarVersionamento(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        })
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// Adiciona Swagger com suporte a JWT e versionamento.
    /// </summary>
    public static IServiceCollection AdicionarSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Folha de Pagamento API",
                Version = "v1",
                Description = "API para processamento de folha de pagamento com INSS, IRRF, FGTS e Consignados.",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Suporte",
                    Email = "suporte@folhapagamento.com.br"
                }
            });

            // Configurar autenticação JWT no Swagger
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Adiciona CORS para desenvolvimento.
    /// </summary>
    public static IServiceCollection AdicionarCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("Desenvolvimento", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });

            options.AddPolicy("Producao", policy =>
            {
                policy.WithOrigins("https://folhapagamento.com.br")
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }
}
