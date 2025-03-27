﻿using System.Text.Json;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Config;

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
        private string _configPath = "";

        private void LoadConfig()
        {
            Config = ConfigManager.Load<PluginConfig>("TeamBalancer");
            _configPath = Path.Combine(ModuleDirectory, $"../../configs/plugins/TeamBalancer/TeamBalancer.json");
        }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            Console.WriteLine(Localizer["config.loaded"]);
        }

        private void SaveConfig()
        {
            var jsonString = JsonSerializer.Serialize(Config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, jsonString);
        }
    }
}
