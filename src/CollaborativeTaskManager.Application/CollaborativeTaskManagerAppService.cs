using CollaborativeTaskManager.Localization;
using Volo.Abp.Application.Services;

namespace CollaborativeTaskManager;

/* Inherit your application services from this class.
 */
public abstract class CollaborativeTaskManagerAppService : ApplicationService
{
    protected CollaborativeTaskManagerAppService()
    {
        LocalizationResource = typeof(CollaborativeTaskManagerResource);
    }
}
