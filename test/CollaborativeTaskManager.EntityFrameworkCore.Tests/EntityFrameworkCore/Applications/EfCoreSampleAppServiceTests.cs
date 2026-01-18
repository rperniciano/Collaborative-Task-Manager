using CollaborativeTaskManager.Samples;
using Xunit;

namespace CollaborativeTaskManager.EntityFrameworkCore.Applications;

[Collection(CollaborativeTaskManagerTestConsts.CollectionDefinitionName)]
public class EfCoreSampleAppServiceTests : SampleAppServiceTests<CollaborativeTaskManagerEntityFrameworkCoreTestModule>
{

}
