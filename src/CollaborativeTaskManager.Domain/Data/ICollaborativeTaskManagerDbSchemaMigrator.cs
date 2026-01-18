using System.Threading.Tasks;

namespace CollaborativeTaskManager.Data;

public interface ICollaborativeTaskManagerDbSchemaMigrator
{
    Task MigrateAsync();
}
