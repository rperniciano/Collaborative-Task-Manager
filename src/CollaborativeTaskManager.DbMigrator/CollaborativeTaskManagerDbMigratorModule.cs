using CollaborativeTaskManager.EntityFrameworkCore;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace CollaborativeTaskManager.DbMigrator;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(CollaborativeTaskManagerEntityFrameworkCoreModule),
    typeof(CollaborativeTaskManagerApplicationContractsModule)
)]
public class CollaborativeTaskManagerDbMigratorModule : AbpModule
{
}
