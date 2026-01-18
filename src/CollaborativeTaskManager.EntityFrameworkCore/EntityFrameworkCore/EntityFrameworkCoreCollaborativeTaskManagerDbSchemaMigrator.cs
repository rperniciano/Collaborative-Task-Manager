using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CollaborativeTaskManager.Data;
using Volo.Abp.DependencyInjection;

namespace CollaborativeTaskManager.EntityFrameworkCore;

public class EntityFrameworkCoreCollaborativeTaskManagerDbSchemaMigrator
    : ICollaborativeTaskManagerDbSchemaMigrator, ITransientDependency
{
    private readonly IServiceProvider _serviceProvider;

    public EntityFrameworkCoreCollaborativeTaskManagerDbSchemaMigrator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task MigrateAsync()
    {
        /* We intentionally resolving the CollaborativeTaskManagerDbContext
         * from IServiceProvider (instead of directly injecting it)
         * to properly get the connection string of the current tenant in the
         * current scope.
         */

        await _serviceProvider
            .GetRequiredService<CollaborativeTaskManagerDbContext>()
            .Database
            .MigrateAsync();
    }
}
