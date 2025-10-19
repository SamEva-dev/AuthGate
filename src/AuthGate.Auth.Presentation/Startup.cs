namespace AuthGate.Auth.Presentation;

using Asp.Versioning.ApiExplorer;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System;
using Serilog;
using AuthGate.Auth.Presentation.Extensions;
using CorrelationId.DependencyInjection;
using CorrelationId;
using AuthGate.Auth.Presentation.Middleware;
using AuthGate.Auth.Presentation.Security;
using Microsoft.AspNetCore.Authorization;

public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // === Controllers & API versioning ===
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddApiVersioning(o =>
            {
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.DefaultApiVersion = new ApiVersion(1, 0);
            })
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerDocumentation();

            // === CORS ===
            services.AddCors(options =>
            {
                options.AddPolicy("CorsPolicy", builder =>
                    builder
                        .WithOrigins(Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
            });

            var key = Encoding.UTF8.GetBytes(Configuration["Jwt:SecretKey"] ?? "DefaultDevSecretKey!");
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    };
                });

        services.AddAuthorization();
        services.AddSignalR();
        services.AddDefaultCorrelationId(options =>
        {
            options.AddToLoggingScope = true;
            options.EnforceHeader = false;
            options.IgnoreRequestHeader = false;
            options.IncludeInResponse = true;
            options.RequestHeader = "X-Correlation-ID";
            options.ResponseHeader = "X-Correlation-ID";
        });

    services.AddScoped<IAuthorizationHandler, HasPermissionHandler>();
    // Policy provider must be registered as singleton; the provider will create scopes when it needs DB access
    services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();

    }

        public void Configure(IApplicationBuilder app, IHostEnvironment env, IApiVersionDescriptionProvider provider)
        {
            app.UseCors("CorsPolicy");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // === Swagger ===
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "swagger";
                foreach (var description in provider.ApiVersionDescriptions)
                    c.SwaggerEndpoint($"{description.GroupName}/swagger.yaml", $"LocaGuest API {description.GroupName}");
            });

            app.UseHttpsRedirection();

       

        app.UseCorrelationId();
        app.UseSerilogRequestLogging(); // log de toutes les requêtes HTTP
        app.Use(async (context, next) =>
        {
            var userId = context.User?.FindFirst("sub")?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                Serilog.Context.LogContext.PushProperty("UserId", userId);
            }
            await next();
        });
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/", () => Results.Json(new
            {
                name = "AuthGate.Auth",
                status = "ok",
                version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
            }));
            // endpoints.MapHealthChecks("/health/live");
            //endpoints.MapHealthChecks("/health/ready");
            endpoints.MapControllers();
            // endpoints.MapHub<SessionHub>("/hubs/sessions");
        });
    }
}
