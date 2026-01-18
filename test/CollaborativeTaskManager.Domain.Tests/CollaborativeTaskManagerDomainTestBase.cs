using Volo.Abp.Modularity;

namespace CollaborativeTaskManager;

/* Inherit from this class for your domain layer tests. */
public abstract class CollaborativeTaskManagerDomainTestBase<TStartupModule> : CollaborativeTaskManagerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{

}
