using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeamBalancer
{
    public partial class TeamBalancer : BasePlugin
    {
        public override string ModuleName => "Team Balancer";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";
        public override string ModuleVersion => "0.0.3";

        public override void Load(bool hotReload)
        {
            // initialize configuration
            LoadConfig();
            SaveConfig();
            // create listeners
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
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

        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null
                || @event.Team == (byte)CsTeam.Spectator
                || @event.Team == (byte)CsTeam.None) return HookResult.Continue;
            // get initial data
            int score_t = GetTeamScore(CsTeam.Terrorist);
            int score_ct = GetTeamScore(CsTeam.CounterTerrorist);
            var (count_t, count_ct) = CountActivePlayers();
            // substract counter depending on team to match current players per team (because this is a Hook)
            if (@event.Team == (byte)CsTeam.Terrorist)
            {
                count_t -= 1;
            }
            else if (@event.Team == (byte)CsTeam.CounterTerrorist)
            {
                count_ct -= 1;
            }
            // check if player should be switched to CT (if T) or vice versa
            if (@event.Team == (byte)CsTeam.Terrorist
                && count_ct <= count_t
                && score_t - score_ct >= 2)
            {
                player.ChangeTeam(CsTeam.CounterTerrorist);
                // inform players
                player.PrintToCenterAlert(Localizer["switch.to_ct_center"].Value
                    .Replace("{player}", player.PlayerName));
                SendGlobalChatMessage(Localizer["switch.to_ct_chat"].Value
                    .Replace("{player}", player.PlayerName));
                // update scoreboard
                var @tmpEvent = new EventNextlevelChanged(true);
                @tmpEvent.FireEvent(false);
            }
            else if (@event.Team == (byte)CsTeam.CounterTerrorist
                        && count_t <= count_ct
                        && score_ct - score_t >= 2)
            {
                player.ChangeTeam(CsTeam.Terrorist);
                // inform players
                player.PrintToCenterAlert(Localizer["switch.to_t_center"].Value
                    .Replace("{player}", player.PlayerName));
                SendGlobalChatMessage(Localizer["switch.to_t_chat"].Value
                    .Replace("{player}", player.PlayerName));
                // update scoreboard
                var @tmpEvent = new EventNextlevelChanged(true);
                @tmpEvent.FireEvent(false);
            }
            return HookResult.Continue;
        }


    }
}
