using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Uow;

namespace CollaborativeTaskManager.EntityFrameworkCore;

[DependsOn(
    typeof(CollaborativeTaskManagerApplicationTestModule),
    typeof(CollaborativeTaskManagerEntityFrameworkCoreModule),
    typeof(AbpEntityFrameworkCoreSqlServerModule)
)]
public class CollaborativeTaskManagerEntityFrameworkCoreTestModule : AbpModule
{
    private SqlConnection? _sqlConnection;
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<FeatureManagementOptions>(options =>
        {
            options.SaveStaticFeaturesToDatabase = false;
            options.IsDynamicFeatureStoreEnabled = false;
        });
        Configure<PermissionManagementOptions>(options =>
        {
            options.SaveStaticPermissionsToDatabase = false;
            options.IsDynamicPermissionStoreEnabled = false;
        });
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();

        ConfigureInMemorySqlServer(context.Services);

    }

    private void ConfigureInMemorySqlServer(IServiceCollection services)
    {
        _sqlConnection = CreateDatabaseAndGetConnection();

        services.Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(context =>
            {
                context.DbContextOptions.UseSqlServer(_sqlConnection);
            });
        });
    }

    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        _sqlConnection?.Dispose();
    }

    private static SqlConnection CreateDatabaseAndGetConnection()
    {
        var connection = new SqlConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CollaborativeTaskManagerDbContext>()
            .UseSqlServer(connection)
            .Options;

        using (var context = new CollaborativeTaskManagerDbContext(options))
        {
            context.GetService<IRelationalDatabaseCreator>().CreateTables();
        }

        return connection;
    }
}
