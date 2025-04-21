using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using System.Text.Json.Serialization;

namespace KnifeFight
{
    public class PluginConfig : BasePluginConfig
    {
        // disabled
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        // debug prints
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        // SFUI string
        [JsonPropertyName("sfui_string")] public string SfuiString { get; set; } = "#SFUI_vote_passed_changelevel";
        // time to vote in seconds
        [JsonPropertyName("vote_time")] public int VoteTime { get; set; } = 15;
        // time to add to round time in seconds
        [JsonPropertyName("extend_time")] public int ExtendTime { get; set; } = 120;
        // make players glow
        [JsonPropertyName("glow")] public bool Glow { get; set; } = true;
        // add beam to players
        [JsonPropertyName("beam")] public bool Beam { get; set; } = true;
        // beam thickness
        [JsonPropertyName("beam_thickness")] public float BeamThickness { get; set; } = 1.5f;
        // beam lifetime
        [JsonPropertyName("beam_lifetime")] public float BeamLifetime { get; set; } = 3f;
        // beam length
        [JsonPropertyName("beam_length")] public float BeamLength { get; set; } = 25f;
        // weapons to give to a player
        [JsonPropertyName("weapons")] public List<string> Weapons { get; set; } = ["weapon_knife"];
        // whether to disallow weapon pick up
        [JsonPropertyName("disallow_weapon_pickup")] public bool DisallowWeaponPickup { get; set; } = true;
        // health to give to a player
        [JsonPropertyName("health")] public int Health { get; set; } = 100;
        // whether to disable bomb spots or not
        [JsonPropertyName("disable_bombspots")] public bool DisableBombspots { get; set; } = true;
        // whether to disable rescue zones or not
        [JsonPropertyName("disable_rescue_zones")] public bool DisableRescueZones { get; set; } = true;
    }

    public partial class KnifeFight : BasePlugin, IPluginConfig<PluginConfig>
    {
        public required PluginConfig Config { get; set; }

        public void OnConfigParsed(PluginConfig config)
        {
            Config = config;
            // update config and write new values from plugin to config file if changed after update
            Config.Update();
            Console.WriteLine(Localizer["core.config"]);
        }
    }
}
