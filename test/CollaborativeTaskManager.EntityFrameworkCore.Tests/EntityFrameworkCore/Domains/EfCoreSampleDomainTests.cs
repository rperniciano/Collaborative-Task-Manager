using CollaborativeTaskManager.Samples;
using Xunit;

namespace CollaborativeTaskManager.EntityFrameworkCore.Domains;

[Collection(CollaborativeTaskManagerTestConsts.CollectionDefinitionName)]
public class EfCoreSampleDomainTests : SampleDomainTests<CollaborativeTaskManagerEntityFrameworkCoreTestModule>
{

}
