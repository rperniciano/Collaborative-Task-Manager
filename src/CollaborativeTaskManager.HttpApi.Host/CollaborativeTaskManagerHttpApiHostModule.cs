using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Extensions.DependencyInjection;
using OpenIddict.Validation.AspNetCore;
using OpenIddict.Server.AspNetCore;
using CollaborativeTaskManager.EntityFrameworkCore;
using CollaborativeTaskManager.MultiTenancy;
using CollaborativeTaskManager.HealthChecks;
using Microsoft.OpenApi.Models;
using Volo.Abp;
using Volo.Abp.Studio;
using Volo.Abp.Account;
using Volo.Abp.Account.Web;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Autofac;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.UI.Navigation.Urls;
using Volo.Abp.VirtualFileSystem;
using Volo.Abp.AspNetCore.Mvc.UI.Bundling;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.Shared;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite;
using Volo.Abp.AspNetCore.Mvc.UI.Theme.LeptonXLite.Bundling;
using Microsoft.AspNetCore.Hosting;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Identity;
using Volo.Abp.OpenIddict;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Studio.Client.AspNetCore;
using Volo.Abp.Security.Claims;
using CollaborativeTaskManager.Hubs;

namespace CollaborativeTaskManager;

[DependsOn(
    typeof(CollaborativeTaskManagerHttpApiModule),
    typeof(AbpStudioClientAspNetCoreModule),
    typeof(AbpAspNetCoreMvcUiLeptonXLiteThemeModule),
    typeof(AbpAutofacModule),
    typeof(AbpAspNetCoreMultiTenancyModule),
    typeof(CollaborativeTaskManagerApplicationModule),
    typeof(CollaborativeTaskManagerEntityFrameworkCoreModule),
    typeof(AbpAccountWebOpenIddictModule),
    typeof(AbpSwashbuckleModule),
    typeof(AbpAspNetCoreSerilogModule)
    )]
public class CollaborativeTaskManagerHttpApiHostModule : AbpModule
{
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();
        var configuration = context.Services.GetConfiguration();

        PreConfigure<OpenIddictBuilder>(builder =>
        {
            builder.AddValidation(options =>
            {
                options.AddAudiences("CollaborativeTaskManager");
                options.UseLocalServer();
                options.UseAspNetCore();
            });
        });

        if (!hostingEnvironment.IsDevelopment())
        {
            PreConfigure<AbpOpenIddictAspNetCoreOptions>(options =>
            {
                options.AddDevelopmentEncryptionAndSigningCertificate = false;
            });

            PreConfigure<OpenIddictServerBuilder>(serverBuilder =>
            {
                serverBuilder.AddProductionEncryptionAndSigningCertificate("openiddict.pfx", configuration["AuthServer:CertificatePassPhrase"]!);
                serverBuilder.SetIssuer(new Uri(configuration["AuthServer:Authority"]!));
            });
        }
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (!configuration.GetValue<bool>("App:DisablePII"))
        {
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }

        if (!configuration.GetValue<bool>("AuthServer:RequireHttpsMetadata"))
        {
            Configure<OpenIddictServerAspNetCoreOptions>(options =>
            {
                options.DisableTransportSecurityRequirement = true;
            });
            
            Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });
        }

        ConfigureStudio(hostingEnvironment);
        ConfigureAuthentication(context);
        ConfigureUrls(configuration);
        ConfigureBundles();
        ConfigureConventionalControllers();
        ConfigureHealthChecks(context);
        ConfigureSwagger(context, configuration);
        ConfigureVirtualFileSystem(context);
        ConfigureCors(context, configuration);
        ConfigureSignalR(context);
    }

    private void ConfigureSignalR(ServiceConfigurationContext context)
    {
        context.Services.AddSignalR();
    }

    private void ConfigureStudio(IHostEnvironment hostingEnvironment)
    {
        if (hostingEnvironment.IsProduction())
        {
            Configure<AbpStudioClientOptions>(options =>
            {
                options.IsLinkEnabled = false;
            });
        }
    }

    private void ConfigureAuthentication(ServiceConfigurationContext context)
    {
        context.Services.ForwardIdentityAuthenticationForBearer(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        context.Services.Configure<AbpClaimsPrincipalFactoryOptions>(options =>
        {
            options.IsDynamicClaimsEnabled = true;
        });
    }

    private void ConfigureUrls(IConfiguration configuration)
    {
        Configure<AppUrlOptions>(options =>
        {
            options.Applications["MVC"].RootUrl = configuration["App:SelfUrl"];
            options.Applications["Angular"].RootUrl = configuration["App:AngularUrl"];
            options.Applications["Angular"].Urls[AccountUrlNames.PasswordReset] = "account/reset-password";
            options.RedirectAllowedUrls.AddRange(configuration["App:RedirectAllowedUrls"]?.Split(',') ?? Array.Empty<string>());
        });
    }

    private void ConfigureBundles()
    {
        Configure<AbpBundlingOptions>(options =>
        {
            options.StyleBundles.Configure(
                LeptonXLiteThemeBundles.Styles.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-styles.css");
                }
            );

            options.ScriptBundles.Configure(
                LeptonXLiteThemeBundles.Scripts.Global,
                bundle =>
                {
                    bundle.AddFiles("/global-scripts.js");
                }
            );
        });
    }


    private void ConfigureVirtualFileSystem(ServiceConfigurationContext context)
    {
        var hostingEnvironment = context.Services.GetHostingEnvironment();

        if (hostingEnvironment.IsDevelopment())
        {
            Configure<AbpVirtualFileSystemOptions>(options =>
            {
                options.FileSets.ReplaceEmbeddedByPhysical<CollaborativeTaskManagerDomainSharedModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}CollaborativeTaskManager.Domain.Shared"));
                options.FileSets.ReplaceEmbeddedByPhysical<CollaborativeTaskManagerDomainModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}CollaborativeTaskManager.Domain"));
                options.FileSets.ReplaceEmbeddedByPhysical<CollaborativeTaskManagerApplicationContractsModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}CollaborativeTaskManager.Application.Contracts"));
                options.FileSets.ReplaceEmbeddedByPhysical<CollaborativeTaskManagerApplicationModule>(Path.Combine(hostingEnvironment.ContentRootPath, $"..{Path.DirectorySeparatorChar}CollaborativeTaskManager.Application"));
            });
        }
    }

    private void ConfigureConventionalControllers()
    {
        Configure<AbpAspNetCoreMvcOptions>(options =>
        {
            options.ConventionalControllers.Create(typeof(CollaborativeTaskManagerApplicationModule).Assembly);
        });
    }

    private static void ConfigureSwagger(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddAbpSwaggerGenWithOidc(
            configuration["AuthServer:Authority"]!,
            ["CollaborativeTaskManager"],
            [AbpSwaggerOidcFlows.AuthorizationCode],
            null,
            options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "CollaborativeTaskManager API", Version = "v1" });
                options.DocInclusionPredicate((docName, description) => true);
                options.CustomSchemaIds(type => type.FullName);
            });
    }

    private void ConfigureCors(ServiceConfigurationContext context, IConfiguration configuration)
    {
        context.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder
                    .WithOrigins(
                        configuration["App:CorsOrigins"]?
                            .Split(",", StringSplitOptions.RemoveEmptyEntries)
                            .Select(o => o.Trim().RemovePostFix("/"))
                            .ToArray() ?? Array.Empty<string>()
                    )
                    .WithAbpExposedHeaders()
                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });
    }

    private void ConfigureHealthChecks(ServiceConfigurationContext context)
    {
        context.Services.AddCollaborativeTaskManagerHealthChecks();
    }


    public override async Task OnApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        var env = context.GetEnvironment();

        // Auto-migrate database - always run to apply pending migrations
        System.Console.WriteLine("[DB] Starting database initialization...");
        try
        {
            using var scope = context.ServiceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CollaborativeTaskManager.EntityFrameworkCore.CollaborativeTaskManagerDbContext>();

            // Try EF Core migrations first
            try
            {
                System.Console.WriteLine("[DB] Running EF Core migrations...");
                await dbContext.Database.MigrateAsync();
                System.Console.WriteLine("[DB] EF Core migrations completed.");
            }
            catch (Exception ex)
            {
                // Log but don't fail - migrations may fail if tables already exist
                System.Console.WriteLine($"[DB] Migration error (may be expected): {ex.Message}");
            }

            // Always ensure ChecklistItems table exists (separate from EF migrations)
            System.Console.WriteLine("[DB] Checking for ChecklistItems table...");
            await EnsureChecklistItemsTableExistsAsync(dbContext);
            System.Console.WriteLine("[DB] ChecklistItems table check completed.");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[DB] Database initialization error: {ex.Message}");
        }
        System.Console.WriteLine("[DB] Database initialization finished.");

        app.UseForwardedHeaders();

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseAbpRequestLocalization();

        if (!env.IsDevelopment())
        {
            app.UseErrorPage();
        }

        app.UseRouting();
        app.MapAbpStaticAssets();
        app.UseAbpStudioLink();
        app.UseAbpSecurityHeaders();
        app.UseCors();
        app.UseAuthentication();
        app.UseAbpOpenIddictValidation();

        if (MultiTenancyConsts.IsEnabled)
        {
            app.UseMultiTenancy();
        }

        app.UseUnitOfWork();
        app.UseDynamicClaims();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseAbpSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CollaborativeTaskManager API");

            var configuration = context.ServiceProvider.GetRequiredService<IConfiguration>();
            options.OAuthClientId(configuration["AuthServer:SwaggerClientId"]);
        });
        app.UseAuditing();
        app.UseAbpSerilogEnrichers();

        // Map SignalR Hub for real-time communication
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHub<BoardHub>("/hubs/board");
        });

        app.UseConfiguredEndpoints();

        System.Console.WriteLine("[SignalR] BoardHub mapped to /hubs/board");
    }

    private static async Task EnsureChecklistItemsTableExistsAsync(CollaborativeTaskManager.EntityFrameworkCore.CollaborativeTaskManagerDbContext dbContext)
    {
        System.Console.WriteLine("[ChecklistItems] Starting table check...");
        try
        {
            // Check if the AppChecklistItems table exists
            var tableExistsSql = @"
                SELECT CASE WHEN EXISTS (
                    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = 'AppChecklistItems'
                ) THEN 1 ELSE 0 END";

            System.Console.WriteLine("[ChecklistItems] Checking if table exists...");

            using var command = dbContext.Database.GetDbConnection().CreateCommand();
            command.CommandText = tableExistsSql;

            if (command.Connection?.State != System.Data.ConnectionState.Open)
            {
                await command.Connection!.OpenAsync();
            }

            var result = await command.ExecuteScalarAsync();
            var tableExists = Convert.ToInt32(result) == 1;

            System.Console.WriteLine($"[ChecklistItems] Table exists: {tableExists}");

            if (!tableExists)
            {
                System.Console.WriteLine("[ChecklistItems] Creating AppChecklistItems table...");

                var createTableSql = @"
                    CREATE TABLE [AppChecklistItems] (
                        [Id] uniqueidentifier NOT NULL,
                        [TaskId] uniqueidentifier NOT NULL,
                        [Text] nvarchar(500) NOT NULL,
                        [IsCompleted] bit NOT NULL,
                        [Order] int NOT NULL,
                        [ExtraProperties] nvarchar(max) NOT NULL,
                        [ConcurrencyStamp] nvarchar(40) NOT NULL,
                        [CreationTime] datetime2 NOT NULL,
                        [CreatorId] uniqueidentifier NULL,
                        [LastModificationTime] datetime2 NULL,
                        [LastModifierId] uniqueidentifier NULL,
                        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
                        [DeleterId] uniqueidentifier NULL,
                        [DeletionTime] datetime2 NULL,
                        CONSTRAINT [PK_AppChecklistItems] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_AppChecklistItems_AppTasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [AppTasks] ([Id]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_AppChecklistItems_TaskId] ON [AppChecklistItems] ([TaskId]);
                ";

                await dbContext.Database.ExecuteSqlRawAsync(createTableSql);
                System.Console.WriteLine("[ChecklistItems] Table created successfully.");
            }
            else
            {
                System.Console.WriteLine("[ChecklistItems] Table already exists, no action needed.");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"[ChecklistItems] ERROR: Could not ensure AppChecklistItems table: {ex.Message}");
            System.Console.WriteLine($"[ChecklistItems] Stack trace: {ex.StackTrace}");
        }
    }
}
