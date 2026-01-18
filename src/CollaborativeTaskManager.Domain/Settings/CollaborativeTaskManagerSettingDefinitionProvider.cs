using Volo.Abp.Settings;

namespace CollaborativeTaskManager.Settings;

public class CollaborativeTaskManagerSettingDefinitionProvider : SettingDefinitionProvider
{
    public override void Define(ISettingDefinitionContext context)
    {
        //Define your own settings here. Example:
        //context.Add(new SettingDefinition(CollaborativeTaskManagerSettings.MySetting1));
    }
}
