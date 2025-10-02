using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;
using PanoramaVoteManagerAPI.Vote;
using PanoramaVoteManagerAPI.Enums;

namespace KnifeFight
{
    public partial class KnifeFight
    {
        [ConsoleCommand("knifefight", "KnifeFight admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandMapVote(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower(System.Globalization.CultureInfo.CurrentCulture))
            {
                case "reload":
                    Config.Reload();
                    command.ReplyToCommand(Localizer["admin.reload"]);
                    break;
                case "test":
                    if (_voteManager == null)
                    {
                        command.ReplyToCommand("vote manager api not found");
                        return;
                    }
                    _vote = new(
                        sfui: Config.SfuiString,
                        text: new Dictionary<string, string> {
                            {"en", $"KNIFE FIGHT TEST COMMAND"},
                        },
                        time: Config.VoteTime,
                        team: -1,
                        playerIDs: [],
                        initiator: 99,
                        minSuccessPercentage: 0.51f,
                        minVotes: 1,
                        flags: VoteFlags.DoNotEndUntilAllVoted,
                        callback: CommandTestOnVoteResult
                    );
                    int startTime = _voteManager.AddVote(_vote);
                    command.ReplyToCommand($"vote will start in approx. {startTime} seconds");
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }

        public void CommandTestOnVoteResult(Vote vote, bool success)
        {
            if (_vote == null)
            {
                return;
            }

            Console.WriteLine($"Vote was {(success ? "successful" : "unsuccessful")} -> {vote.Voters.Count} of {vote.PlayerIDs.Count} have voted");
        }
    }
}
