using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeamBalancer
{
    public partial class TeamBalancer : BasePlugin
    {
        public override string ModuleName => "Team Balancer";
        public override string ModuleAuthor => "Jon-Mailes Graeffe <mail@jonni.it> / Kalle <kalle@kandru.de>";

        private bool _halfTime = false;

        public override void Load(bool hotReload)
        {
            // initialize configuration
            LoadConfig();
            SaveConfig();
            // create listeners
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            RegisterEventHandler<EventAnnouncePhaseEnd>(OnAnnouncePhaseEnd);
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
                || !player.IsValid
                || player.IsBot
                || @event.Team == (byte)CsTeam.Spectator
                || @event.Team == (byte)CsTeam.None
                || (_halfTime == true && @event.Oldteam != (byte)CsTeam.Spectator)
                || (_halfTime == true && @event.Oldteam != (byte)CsTeam.None)) return HookResult.Continue;
            // get initial data
            int score_t = GetTeamScore(CsTeam.Terrorist);
            int score_ct = GetTeamScore(CsTeam.CounterTerrorist);
            var (count_t, count_ct) = CountActivePlayers();
            // substract counter depending on team to match current players per team (because this is a Hook)
            if (@event.Oldteam == (byte)CsTeam.Terrorist)
            {
                count_t -= 1;
            }
            else if (@event.Oldteam == (byte)CsTeam.CounterTerrorist)
            {
                count_ct -= 1;
            }
            // check if player should be switched to CT (if T) or vice versa
            if (@event.Team == (byte)CsTeam.Terrorist
                && count_ct <= count_t
                && score_t - score_ct >= 2)
            {
                AddTimer(0f, () =>
                {
                    if (player == null || !player.IsValid) return;
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                    var @tmpEvent = new EventNextlevelChanged(true);
                    @tmpEvent.FireEvent(false);
                });
                // inform players
                player.PrintToCenterAlert(Localizer["switch.to_ct_center"].Value
                    .Replace("{player}", player.PlayerName));
                SendGlobalChatMessage(Localizer["switch.to_ct_chat"].Value
                    .Replace("{player}", player.PlayerName));
                // update scoreboard
            }
            else if (@event.Team == (byte)CsTeam.CounterTerrorist
                        && count_t <= count_ct
                        && score_ct - score_t >= 2)
            {
                AddTimer(0f, () =>
                {
                    if (player == null || !player.IsValid) return;
                    player.ChangeTeam(CsTeam.Terrorist);
                    var @tmpEvent = new EventNextlevelChanged(true);
                    @tmpEvent.FireEvent(false);
                });
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

        public HookResult OnAnnouncePhaseEnd(EventAnnouncePhaseEnd @event, GameEventInfo info)
        {
            // half time seems to occure exactly 5 seconds after this event.
            // enable half time fix
            AddTimer(4f, () =>
            {
                _halfTime = true;
            });
            // disable half time fix
            AddTimer(6f, () =>
            {
                _halfTime = false;
            });
            return HookResult.Continue;
        }
    }
}
