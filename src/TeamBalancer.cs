using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.IO.Compression;

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

        public Tuple<int, int> CountActivePlayers()
        {
            int count_t = 0;
            int count_ct = 0;
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.Team == CsTeam.CounterTerrorist)
                {
                    count_ct++;
                }
                else if (player.Team == CsTeam.Terrorist)
                {
                    count_t++;
                }
            }
            return Tuple.Create(count_t, count_ct);
        }
    }
}
