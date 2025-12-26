using System.Text.Json;

namespace Logic.Data;

public class WorkshopEntry : IWorkshopEntry
{
    public long id;
    public string path;
    public DateTime? creationDate;

    public string? iconPath;
    public string? title;
    public string[]? tags;
    public string? type;
    public Properties[]? properties;

    private int decodeStatus;

    public long getId => id;
    public string? getIconPath => string.IsNullOrEmpty(path) ? string.Empty : Path.Combine(path, iconPath!);
    public string? getTitle => title;

    public WorkshopEntry(long id, string path, DateTime? creationDate)
    {
        this.id = id;
        this.path = path;
        this.creationDate = creationDate;

        decodeStatus = 0;
    }

    private async Task<JsonDocument?> ReadJson()
    {
        string projectPath = Path.Combine(path, "project.json");

        if (!File.Exists(projectPath))
            return null;

        using StreamReader reader = new StreamReader(projectPath);
        string json = await reader.ReadToEndAsync();

        return JsonDocument.Parse(json);
    }

    public async Task<bool> DecodeBasic(JsonDocument? doc = null)
    {
        if (decodeStatus >= 1)
            return true;

        decodeStatus = 1;

        doc ??= await ReadJson();
        if (doc == null) return false;

        title = doc.RootElement.GetProperty("title").GetString();
        iconPath = doc.RootElement.GetProperty("preview").GetString();
        tags = doc.RootElement.GetProperty("tags").Deserialize<string[]>();

        return true;
    }

    public async Task<bool> Decode(JsonDocument? doc = null)
    {
        if (decodeStatus >= 2)
            return true;

        doc ??= await ReadJson();

        if (decodeStatus < 1)
            await DecodeBasic(doc);

        decodeStatus = 2;
        if (doc == null) return false;

        if (doc.RootElement.TryGetProperty("general", out JsonElement general))
        {
            JsonElement properties = general.GetProperty("properties");

            List<Properties> temp = new List<Properties>();

            foreach (JsonProperty prop in properties.EnumerateObject())
                temp.Add(new Properties(prop));

            this.properties = temp.ToArray();
        }

        return true;
    }


    public class Properties
    {
        public string? propertyName;
        public int? order;
        public string? text;
        public PropertyType? type;
        public string? value;

        public (string label, string value)[]? comboOptions;

        public double? max;
        public double? min;
        public double? precision;
        public double? step;

        public Properties(JsonProperty parent)
        {
            propertyName = parent.Name;
            order = parent.Value.GetProperty("order").GetInt32();
            text = parent.Value.GetProperty("text").GetString();

            if (parent.Value.TryGetProperty("type", out JsonElement el))
            {
                string? typeName = el.GetString();

                switch (typeName?.ToLower())
                {
                    case "slider":
                        type = PropertyType.slider;
                        value = parent.Value.GetProperty("value").GetDouble().ToString();

                        parent.Value.TryGetDoubleProperty("max", out max);
                        parent.Value.TryGetDoubleProperty("min", out min);
                        parent.Value.TryGetDoubleProperty("precision", out precision);
                        parent.Value.TryGetDoubleProperty("step", out step);
                        break;

                    case "color":
                        type = PropertyType.colour;
                        break;

                    case "bool":
                        type = PropertyType.boolean;
                        value = parent.Value.GetProperty("value").GetBoolean() == true ? "1" : "0";
                        return;

                    case "combo":
                        type = PropertyType.combo;
                        comboOptions = parent.Value.GetProperty("options").EnumerateArray().Select(o => (o.GetProperty("label").ToString(), o.GetProperty("value").ToString())).ToArray();
                        break;

                    case "textinput":
                        type = PropertyType.text_input;
                        break;

                    case "scenetexture":
                        type = PropertyType.scene_texture;
                        break;

                    default:
                        type = PropertyType.INVALID;
                        Console.WriteLine($"Couldnt match type - '{typeName}'");
                        return;
                }

                try
                {
                    if (parent.Value.TryGetProperty("value", out el))
                    {
                        value = el.GetString();
                    }
                }
                catch
                {
                    Console.WriteLine($"Unhandled param type for - {type}");
                }
            }
            else
            {
                type = PropertyType.label;
            }

        }
    }

    public enum PropertyType
    {
        INVALID,
        colour,
        boolean,
        combo,
        text_input,
        scene_texture,
        label,
        slider
    }
}
