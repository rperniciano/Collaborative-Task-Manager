using CollaborativeTaskManager.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace CollaborativeTaskManager.Controllers;

/* Inherit your controllers from this class.
 */
public abstract class CollaborativeTaskManagerController : AbpControllerBase
{
    protected CollaborativeTaskManagerController()
    {
        LocalizationResource = typeof(CollaborativeTaskManagerResource);
    }
}
