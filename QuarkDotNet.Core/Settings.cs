using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace QuarkDotNet.Core
{
    public class Settings
    {
        const string SettingsFileName = "Settings.json";

        public class Model
        {
            public bool IsLogVisible { get; set; }

            public IList<string> Paths { get; set; }
        }

        public Model Get()
        {
            var model = new Model();

            if (File.Exists(SettingsFileName))
            {
                var jsonString = File.ReadAllText(SettingsFileName);
                model = JsonSerializer.Deserialize<Model>(jsonString);
            }

            if (model.Paths == null)
            {
                model.Paths = new List<string>()
                {
                    "e:\\switch roms",
                };
            }

            return model;
        }

        public void Set(Model model)
        {
            var jsonString = JsonSerializer.Serialize(model);
            File.WriteAllText("Settings.json", jsonString);
        }
    }
}
