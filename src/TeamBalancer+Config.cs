using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace TeamBalancer
{
    public class PluginConfig : BasePluginConfig
    {
        // disable update checks completely
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // ignore bots when balancing teams
        [JsonPropertyName("ignore_bots")] public bool IgnoreBots { get; set; } = true;
        // minimum score difference to switch teams
        [JsonPropertyName("min_score_difference")] public int MinScoreDifference { get; set; } = 1;
        // allowed player difference upon team switch (default: 1)
        [JsonPropertyName("max_player_difference")] public int MaxPlayerDifference { get; set; } = 1;
    }

    public partial class TeamBalancer : BasePlugin, IPluginConfig<PluginConfig>
    {
        public PluginConfig Config { get; set; } = null!;

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Console.WriteLine(Localizer["config.loaded"]);
        }
    }
}
