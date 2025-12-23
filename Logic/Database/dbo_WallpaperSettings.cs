using CSharpSqliteORM.Structure;

namespace Logic.Database;

public class dbo_WallpaperSettings : IDatabase_Table
{
    public static string tableName => "wallpaperSettings";

    public required long wallpaperId { get; set; }
    public required string settingKey { get; set; }
    public string? settingValue { get; set; }

    public static Database_Column[] getColumns => [
        new Database_Column() { columnName = nameof(wallpaperId), columnType = Database_ColumnType.INTEGER, allowNull = false },
        new Database_Column() { columnName = nameof(settingKey), columnType = Database_ColumnType.TEXT, allowNull = false },
        new Database_Column() { columnName = nameof(settingValue), columnType = Database_ColumnType.TEXT, allowNull = true },
    ];
}
