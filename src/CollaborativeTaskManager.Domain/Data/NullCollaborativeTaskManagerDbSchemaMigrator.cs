using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace CollaborativeTaskManager.Data;

/* This is used if database provider does't define
 * ICollaborativeTaskManagerDbSchemaMigrator implementation.
 */
public class NullCollaborativeTaskManagerDbSchemaMigrator : ICollaborativeTaskManagerDbSchemaMigrator, ITransientDependency
{
    public Task MigrateAsync()
    {
        return Task.CompletedTask;
    }
}
