using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Extensions;
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
            // update configuration on disk to reflect latest changes from plugin
            Config.Update();
            // create listeners
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
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
            // remove listeners
            RemoveListener<Listeners.OnMapStart>(OnMapStart);
            RemoveListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            DeregisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            DeregisterEventHandler<EventAnnouncePhaseEnd>(OnAnnouncePhaseEnd);
            Console.WriteLine(Localizer["core.unload"]);
        }

        public void OnMapStart(string mapName)
        {
            AddTimer(3f, () => SetServerCvars());
        }

        public void OnServerHibernationUpdate(bool hibernating)
        {
            if (!hibernating)
            {
                AddTimer(3f, () => SetServerCvars());
            }
        }

        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            // check if player is valid
            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV ||
            // check if player is spectator or none (still connecting)
            @event.Team == (int)CsTeam.Spectator || @event.Team == (int)CsTeam.None ||
            // check if halftime and ignore
            (_halfTime && @event.Oldteam != (int)CsTeam.Spectator && @event.Oldteam != (int)CsTeam.None))
                return HookResult.Continue;

            // Get initial data
            int scoreT = GetTeamScore(CsTeam.Terrorist);
            int scoreCT = GetTeamScore(CsTeam.CounterTerrorist);
            var (countT, countCT) = CountActivePlayers();

            // Adjust player counts based on old team
            if (@event.Oldteam == (int)CsTeam.Terrorist) countT--;
            else if (@event.Oldteam == (int)CsTeam.CounterTerrorist) countCT--;

            if (@event.Team == (int)CsTeam.Terrorist
                && !IsAllowedToSwitchToTeam(countT, countCT, scoreT, scoreCT))
            {
                SwitchPlayerTeam(player, CsTeam.CounterTerrorist);
            }
            else if (@event.Team == (int)CsTeam.CounterTerrorist
                && !IsAllowedToSwitchToTeam(countCT, countT, scoreCT, scoreT))
            {
                SwitchPlayerTeam(player, CsTeam.Terrorist);
            }
            return HookResult.Continue;
        }

        private void SetServerCvars()
        {
            // disable autoteambalance to make this plugin work
            Server.ExecuteCommand("mp_autoteambalance 0");
            // disable limitteams by taking into account the bot change behaviour when value = 0
            if (Config.IgnoreBots)
                Server.ExecuteCommand($"mp_limitteams 0");
            else
                Server.ExecuteCommand($"mp_limitteams 99");
        }

        private bool IsAllowedToSwitchToTeam(int targetCount, int sourceCount, int targetScore, int sourceScore)
        {
            // Rule 1: Ensure teams are balanced in terms of player count
            if (targetCount >= sourceCount + Config.MaxPlayerDifference)
                return false;
            // Rule 2: Enforce joining the team with less score if the score difference is at least 2
            if (targetScore - sourceScore >= Config.MinScoreDifference // check if new team score is higher than old team score
                && targetCount > sourceCount - Config.MaxPlayerDifference) // check if new team has less players than old team
                return false;
            return true;
        }

        private void SwitchPlayerTeam(CCSPlayerController player, CsTeam newTeam)
        {
            Server.NextFrame(() =>
            {
                // needs double nextframe to allow player gui to update properly
                Server.NextFrame(() =>
                {
                    if (player == null || !player.IsValid) return;
                    player.ChangeTeam(newTeam);
                });
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
