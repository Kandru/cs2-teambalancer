using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace TeamBalancer
{
    public partial class TeamBalancer : BasePlugin
    {
        public void SendGlobalChatMessage(string message, float delay = 0)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.IsBot) continue;
                AddTimer(delay, () => player.PrintToChat(message));
            }
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

        public int GetTeamScore(CsTeam team)
        {
            var teamManagers = Utilities.FindAllEntitiesByDesignerName<CCSTeam>("cs_team_manager");

            foreach (var teamManager in teamManagers)
            {
                if ((int)team == teamManager.TeamNum)
                {
                    return teamManager.Score;
                }
            }

            return 0;
        }
    }
}
