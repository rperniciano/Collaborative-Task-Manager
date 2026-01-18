using CollaborativeTaskManager.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.MultiTenancy;

namespace CollaborativeTaskManager.Permissions;

public class CollaborativeTaskManagerPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(CollaborativeTaskManagerPermissions.GroupName);

        //Define your own permissions here. Example:
        //myGroup.AddPermission(CollaborativeTaskManagerPermissions.MyPermission1, L("Permission:MyPermission1"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<CollaborativeTaskManagerResource>(name);
    }
}
