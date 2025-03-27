using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
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
            CCSPlayerController? player = @event.Userid;
            // check if player is valid
            if (player == null || !player.IsValid || player.IsBot ||
            // check if player is spectator or none (still connecting)
            @event.Team == (byte)CsTeam.Spectator || @event.Team == (byte)CsTeam.None ||
            // check if halftime and ignore
            (_halfTime && @event.Oldteam != (byte)CsTeam.Spectator && @event.Oldteam != (byte)CsTeam.None))
                return HookResult.Continue;

            // Get initial data
            int scoreT = GetTeamScore(CsTeam.Terrorist);
            int scoreCT = GetTeamScore(CsTeam.CounterTerrorist);
            var (countT, countCT) = CountActivePlayers();

            // Adjust player counts based on old team
            if (@event.Oldteam == (byte)CsTeam.Terrorist) countT--;
            else if (@event.Oldteam == (byte)CsTeam.CounterTerrorist) countCT--;

            // Determine if player should switch teams
            if (@event.Team != (byte)CsTeam.Terrorist
                && ShouldSwitchToTeam(@event.Team, CsTeam.Terrorist, countCT, countT, scoreT, scoreCT))
            {
                SwitchPlayerTeam(player, CsTeam.CounterTerrorist);
            }
            else if (@event.Team != (byte)CsTeam.CounterTerrorist
                && ShouldSwitchToTeam(@event.Team, CsTeam.CounterTerrorist, countT, countCT, scoreCT, scoreT))
            {
                SwitchPlayerTeam(player, CsTeam.Terrorist);
            }

            return HookResult.Continue;
        }

        private bool ShouldSwitchToTeam(int currentTeam, CsTeam targetTeam, int targetCount, int sourceCount, int sourceScore, int targetScore)
        {
            // Rule 1: Ensure teams are balanced in terms of player count
            if (targetCount > sourceCount + 1)
                return false;
            // Rule 2: Enforce joining the team with less score if the score difference is at least 2
            if (sourceScore - targetScore >= Config.MinScoreDifference && currentTeam != (byte)targetTeam)
                return false;

            return true;
        }

        private void SwitchPlayerTeam(CCSPlayerController player, CsTeam newTeam)
        {
            Server.NextFrame(() =>
            {
                if (player == null || !player.IsValid) return;
                player.ChangeTeam(newTeam);
                var tmpEvent = new EventNextlevelChanged(true);
                tmpEvent.FireEvent(false);
            });
            // get team
            string team = "";
            if (newTeam == CsTeam.Terrorist)
                team = "t";
            else if (newTeam == CsTeam.CounterTerrorist)
                team = "ct";
            // Inform player
            player.PrintToCenterAlert(Localizer[$"switch.to_{team}_center"].Value.Replace("{player}", player.PlayerName));
            // Inform other players
            SendGlobalChatMessage(Localizer[$"switch.to_{team}_chat"].Value.Replace("{player}", player.PlayerName));
        }

        public HookResult OnAnnouncePhaseEnd(EventAnnouncePhaseEnd @event, GameEventInfo info)
        {
            _halfTime = true;
            // check for half time player swap and ignore teamswap
            ConVar? mpTeamIntroTime = ConVar.Find("mp_team_intro_time");
            // disable after mpTeamIntroTime finished
            if (mpTeamIntroTime != null && mpTeamIntroTime.GetPrimitiveValue<float>() > 0.0f)
            {
                mpTeamIntroTime.GetPrimitiveValue<float>();
                // disable half time
                AddTimer(mpTeamIntroTime.GetPrimitiveValue<float>() + 0.5f, () =>
                {
                    _halfTime = false;
                });
            }
            else
            {
                ConVar? mpHalfTimeDuration = ConVar.Find("mp_halftime_duration");
                if (mpHalfTimeDuration != null && mpHalfTimeDuration.GetPrimitiveValue<float>() > 0.0f)
                {
                    // disable half time
                    AddTimer(mpHalfTimeDuration.GetPrimitiveValue<float>() + 0.5f, () =>
                    {
                        _halfTime = false;
                    });
                }
                else
                {
                    // disable half time
                    AddTimer(1f, () =>
                    {
                        _halfTime = false;
                    });
                }
            }
            return HookResult.Continue;
        }
    }
}
