using Volo.Abp.Modularity;

namespace CollaborativeTaskManager;

public abstract class CollaborativeTaskManagerApplicationTestBase<TStartupModule> : CollaborativeTaskManagerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
