using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeamBalancer
{
    public partial class TeamBalancer : BasePlugin
    {
        public override string ModuleName => "Team Balancer";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.0.1";

        public override void Load(bool hotReload)
        {
            // initialize configuration
            LoadConfig();
            UpdateConfig();
            SaveConfig();
            // print message if hot reload
            if (hotReload)
            {
                Console.WriteLine(Localizer["core.hotreload"]);
            }
        }

        public override void Unload(bool hotReload)
        {
            Console.WriteLine(Localizer["core.unload"]);
        }
    }
}
