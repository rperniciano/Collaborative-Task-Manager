using Microsoft.Extensions.Localization;
using CollaborativeTaskManager.Localization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Ui.Branding;

namespace CollaborativeTaskManager;

[Dependency(ReplaceServices = true)]
public class CollaborativeTaskManagerBrandingProvider : DefaultBrandingProvider
{
    private IStringLocalizer<CollaborativeTaskManagerResource> _localizer;

    public CollaborativeTaskManagerBrandingProvider(IStringLocalizer<CollaborativeTaskManagerResource> localizer)
    {
        _localizer = localizer;
    }

    public override string AppName => _localizer["AppName"];
}
