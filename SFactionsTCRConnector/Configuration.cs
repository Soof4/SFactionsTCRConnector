using Newtonsoft.Json;
using TShockAPI;

namespace SFactionsTCRConnector
{
    public class Configuration
    {
        public static string ConfigPath = Path.Combine(TShock.SavePath, "SFactionsTCRConnectorConfig.json");

#pragma warning disable CS0414

        public string ChatFormat = "**{5}{1}{2}{3}:** {4}";
        public string ChatFactionNameOpeningParenthesis = "[";
        public string ChatFactionNameClosingParanthesis = "] ";
        public string ChatFormatHelp = "{5} = Faction name, {1} = Prefix of player's group, {2} = Player's name, {3} = Suffix of player's group, {4} = Message";

#pragma warning restore CS0414

        public static Configuration Reload()
        {
            Configuration? c = null;

            if (File.Exists(ConfigPath))
            {
                c = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigPath));
            }

            if (c == null)
            {
                c = new Configuration();
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(c, Formatting.Indented));
            }

            return c;
        }
    }
}