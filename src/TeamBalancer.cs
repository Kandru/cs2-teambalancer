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

        private bool _halfTime;

        public override void Load(bool hotReload)
        {
            // update configuration on disk to reflect latest changes from plugin
            Config.Update();
            // create listeners
            RegisterListener<Listeners.OnMapStart>(OnMapStart);
            RegisterListener<Listeners.OnServerHibernationUpdate>(OnServerHibernationUpdate);
            RegisterEventHandler<EventPlayerTeam>(OnPlayerTeam);
            RegisterEventHandler<EventAnnouncePhaseEnd>(OnAnnouncePhaseEnd);
            RegisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
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
            DeregisterEventHandler<EventWarmupEnd>(OnWarmupEnd);
            Console.WriteLine(Localizer["core.unload"]);
        }

        public void OnMapStart(string mapName)
        {
            _ = AddTimer(3f, SetServerCvars);
        }

        public void OnServerHibernationUpdate(bool hibernating)
        {
            if (!hibernating)
            {
                _ = AddTimer(3f, SetServerCvars);
            }
        }

        public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            // check if player is valid
            if (player == null || !player.IsValid
            // ignore bots und hltv
            || player.IsBot || player.IsHLTV ||
            // check if player is spectator or none (still connecting)
            @event.Team == (int)CsTeam.Spectator || @event.Team == (int)CsTeam.None ||
            // check if halftime and ignore
            (_halfTime && @event.Oldteam != (int)CsTeam.Spectator && @event.Oldteam != (int)CsTeam.None))
            {
                return HookResult.Continue;
            }

            // Get initial data
            int scoreT = GetTeamScore(CsTeam.Terrorist);
            int scoreCT = GetTeamScore(CsTeam.CounterTerrorist);
            (int countT, int countCT) = CountActivePlayers();

            // Adjust player counts based on old team
            if (@event.Oldteam == (int)CsTeam.Terrorist)
            {
                countT--;
            }
            else if (@event.Oldteam == (int)CsTeam.CounterTerrorist)
            {
                countCT--;
            }

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

        private static void SetServerCvars()
        {
            // disable autoteambalance to make this plugin work
            Server.ExecuteCommand("mp_autoteambalance 0");
            // always disable limitteams to allow teambalancer to handle all player switches correctly
            Server.ExecuteCommand("mp_limitteams 0");
        }

        private bool IsAllowedToSwitchToTeam(int targetCount, int sourceCount, int targetScore, int sourceScore)
        {
            int playerCountDifference = targetCount - sourceCount;

            // Priority 1: Enforce player count balance
            // Block if target team already has MaxPlayerDifference or more players than source team
            if (playerCountDifference >= Config.MaxPlayerDifference)
            {
                return false;
            }

            // Priority 2: Enforce score balance when player counts are within acceptable range
            // Only apply score check when teams are relatively balanced in player count
            if (Math.Abs(playerCountDifference) < Config.MaxPlayerDifference)
            {
                int scoreLeadOfTargetTeam = targetScore - sourceScore;
                // Block if trying to join team with significantly higher score
                if (scoreLeadOfTargetTeam >= Config.MinScoreDifference)
                {
                    return false;
                }
            }

            // Allow switch: either joining smaller team, or teams are balanced in both count and score
            return true;
        }

        private void SwitchPlayerTeam(CCSPlayerController player, CsTeam newTeam)
        {
            Server.NextFrame(() =>
            {
                // needs double nextframe to allow player gui to update properly
                Server.NextFrame(() =>
                {
                    if (player == null || !player.IsValid)
                    {
                        return;
                    }

                    player.ChangeTeam(newTeam);
                });
            });
            // get team
            string team = "";
            if (newTeam == CsTeam.Terrorist)
            {
                team = "t";
            }
            else if (newTeam == CsTeam.CounterTerrorist)
            {
                team = "ct";
            }
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
            if (mpTeamIntroTime != null)
            {
                float introTime = mpTeamIntroTime.GetPrimitiveValue<float>();
                if (introTime > 0.0f)
                {
                    // disable half time
                    _ = AddTimer(introTime + 0.5f, () =>
                    {
                        _halfTime = false;
                    });
                    return HookResult.Continue;
                }
            }

            ConVar? mpHalfTimeDuration = ConVar.Find("mp_halftime_duration");
            if (mpHalfTimeDuration != null)
            {
                float halfTime = mpHalfTimeDuration.GetPrimitiveValue<float>();
                if (halfTime > 0.0f)
                {
                    // disable half time
                    _ = AddTimer(halfTime + 0.5f, () =>
                    {
                        _halfTime = false;
                    });
                    return HookResult.Continue;
                }
            }

            // disable half time with default fallback
            _ = AddTimer(1f, () =>
            {
                _halfTime = false;
            });
            return HookResult.Continue;
        }

        public HookResult OnWarmupEnd(EventWarmupEnd @event, GameEventInfo info)
        {
            if (!Config.ScrambleTeamsAfterWarmup)
            {
                return HookResult.Continue;
            }
            ScrambleTeams();
            return HookResult.Continue;
        }

        private static void ScrambleTeams()
        {
            List<CCSPlayerController> players = [.. Utilities.GetPlayers().Where(static p => p.IsValid && !p.IsBot && !p.IsHLTV && (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))];

            if (players.Count < 2)
            {
                return;
            }

            (int countT, int countCT) = CountActivePlayers();
            int balancedCount = players.Count / 2;

            List<CCSPlayerController> tPlayers = [.. players.Where(static p => p.Team == CsTeam.Terrorist)];
            List<CCSPlayerController> ctPlayers = [.. players.Where(static p => p.Team == CsTeam.CounterTerrorist)];

            if (tPlayers.Count > balancedCount)
            {
                int toMove = tPlayers.Count - balancedCount;
                foreach (CCSPlayerController? player in tPlayers.Take(toMove))
                {
                    player.ChangeTeam(CsTeam.CounterTerrorist);
                }
            }
            else if (ctPlayers.Count > balancedCount)
            {
                int toMove = ctPlayers.Count - balancedCount;
                foreach (CCSPlayerController? player in ctPlayers.Take(toMove))
                {
                    player.ChangeTeam(CsTeam.Terrorist);
                }
            }
        }
    }
}
