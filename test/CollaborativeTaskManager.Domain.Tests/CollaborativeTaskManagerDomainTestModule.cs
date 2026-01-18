using Volo.Abp.Modularity;

namespace CollaborativeTaskManager;

[DependsOn(
    typeof(CollaborativeTaskManagerDomainModule),
    typeof(CollaborativeTaskManagerTestBaseModule)
)]
public class CollaborativeTaskManagerDomainTestModule : AbpModule
{

}
