using Volo.Abp.Modularity;

namespace CollaborativeTaskManager;

[DependsOn(
    typeof(CollaborativeTaskManagerApplicationModule),
    typeof(CollaborativeTaskManagerDomainTestModule)
)]
public class CollaborativeTaskManagerApplicationTestModule : AbpModule
{

}
