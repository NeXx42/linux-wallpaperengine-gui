using CSharpSqliteORM;

namespace Logic;

public static class DatabaseManager
{
    public static Database_Manager.DatabaseInstance? db { get; private set; }

    public static async Task Init(string path)
    {
        db = new Database_Manager.DatabaseInstance();
        await db.Init(path);
    }
}
