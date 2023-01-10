using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLauncherWpf
{
    public class Options
    {
        public DisplayType DisplayType { get; set; }

        public string[]? SpecificNames { get; set; }


        public static Options? LoadFrom(string filepath)
        {
            string json = File.ReadAllText(filepath);
            return JsonSerializer.Deserialize<Options>(json);
        }

        public static void SaveTo(Options options, string filepath)
        {
            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions
            {
                WriteIndented= true,
            });
            File.WriteAllText(filepath, json);
        }
    }

    public enum DisplayType
    {
        FileName,
        DirectoryName,
        SpecificNames,
    }
}
