using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;
using PanoramaVoteManagerAPI;
using PanoramaVoteManagerAPI.Vote;
using PanoramaVoteManagerAPI.Enums;

namespace KnifeFight
{
    public partial class KnifeFight : BasePlugin
    {
        public override string ModuleName => "CS2 KnifeFight";
        public override string ModuleAuthor => "Kalle <kalle@kandru.de>";

        private const float PlayerHeightOffset = 25f;

        private static PluginCapability<IPanoramaVoteManagerAPI> VoteAPI { get; } = new("panoramavotemanager:api");
        private IPanoramaVoteManagerAPI? _voteManager;
        private Vote? _vote;
        private bool _isActive;
        private Vector _lastCtPos = Vector.Zero;
        private Vector _lastTPos = Vector.Zero;

        public override void Load(bool hotReload)
        {
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        }

        public override void OnAllPluginsLoaded(bool hotReload)
        {
            try
            {
                _voteManager = VoteAPI.Get();
                Console.WriteLine($"Successfully got PanoramaVoteManager API");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get PanoramaVoteManager API: {ex.Message}");
            }
        }

        public override void Unload(bool hotReload)
        {
            DeregisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            DeregisterEventHandler<EventRoundEnd>(OnRoundEnd);
            VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Unhook(OnWeaponCanAcquire, HookMode.Pre);
            RemoveListener<Listeners.OnTick>(OnTick);
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            if ((bool)GetGameRule("WarmupPeriod")! || _isActive || IsBombPlanted())
            {
                return HookResult.Continue;
            }

            CCSPlayerController? victim = @event.Userid;
            if (victim == null)
            {
                return HookResult.Continue;
            }

            if (!Config.Enabled
                || CountPlayersAlive(CsTeam.CounterTerrorist) != 1
                || CountPlayersAlive(CsTeam.Terrorist) != 1)
            {
                return HookResult.Continue;
            }

            Server.NextFrame(StartVote);
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _isActive = false;
            if (_vote != null)
            {
                _ = (_voteManager?.RemoveVote(_vote));
                _vote = null;
            }
            ToggleBombspots(true);
            ToggleRescueZones(true);
            return HookResult.Continue;
        }

        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            if (!_isActive)
            {
                return HookResult.Continue;
            }

            CCSWeaponBaseVData vdata = VirtualFunctions.GetCSWeaponDataFromKey.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            return vdata == null ? HookResult.Continue : vdata.Name.Contains("knife") ? HookResult.Continue : HookResult.Stop;
        }

        public void StartVote()
        {
            // check amount of players alive
            List<CCSPlayerController> alivePlayers = GetAlivePlayers();
            // stop if not enough players are alive
            if (alivePlayers.Count != 2)
            {
                return;
            }
            // use vote manager if possible
            if (_voteManager != null)
            {
                DebugPrint("vote-based knifefight");
                // announce knife fight for all
                Server.PrintToChatAll(Localizer["knifefight.vote.started"].Value
                    .Replace("{name1}", alivePlayers[0].PlayerName)
                    .Replace("{name2}", alivePlayers[1].PlayerName)
                );
                _vote = new(
                    sfui: Config.SfuiString,
                    text: new Dictionary<string, string> {
                {"en", $"KNIFE FIGHT?"},
                {"de", $"MESSERKAMPF?"},
                    },
                    time: Config.VoteTime,
                    team: -1,
                    playerIDs: [.. alivePlayers.Select(static player => player.UserId!.Value)],
                    initiator: 99,
                    minSuccessPercentage: 0.51f,
                    minVotes: 1,
                    flags: VoteFlags.DoNotEndUntilAllVoted,
                    callback: OnVoteResult
                );
                int startTime = _voteManager.AddVote(_vote);
                DebugPrint($"vote will start in approx. {startTime} seconds");
            }
            else
            {
                DebugPrint("command-based knifefight");
                // print debug message if PanoramaVoteManager API is not available
                Server.PrintToChatAll(Localizer["core.debugprint"].Value.Replace("{message}", "PanoramaVoteManager API not available. Please install!"));
                // TODO: not yet implemented
            }
        }

        public void OnVoteResult(Vote vote, bool success)
        {
            if (_vote == null)
            {
                return;
            }

            DebugPrint($"Vote was {(success ? "successful" : "unsuccessful")} -> {vote.Voters.Count} of {vote.PlayerIDs.Count} have voted");
            if (!_isActive && success)
            {
                _isActive = true;
                InitializeKnifeFight();
            }
            else
            {
                Server.PrintToChatAll(Localizer["knifefight.vote.failed"]);
            }
        }

        private void InitializeKnifeFight()
        {
            if (!_isActive || IsBombPlanted())
            {
                return;
            }

            List<CCSPlayerController> alivePlayers = GetAlivePlayers();
            if (alivePlayers.Count != 2)
            {
                return;
            }

            foreach (CCSPlayerController player in alivePlayers)
            {
                if (Config.Glow)
                {
                    MakePlayerGlow(player);
                }

                player.RemoveWeapons();

                Server.NextFrame(() =>
                {
                    if (!IsPlayerValid(player))
                    {
                        return;
                    }

                    foreach (string weapon in Config.Weapons)
                    {
                        _ = player.GiveNamedItem(weapon);
                    }

                    player.Pawn.Value!.MaxHealth = Config.Health;
                    player.Pawn.Value.Health = Config.Health;
                    Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
                });

                player.PrintToCenterAlert(Localizer["knifefight.start"]);
            }

            if (Config.DisableBombspots)
            {
                ToggleBombspots(false);
            }

            if (Config.DisableRescueZones)
            {
                ToggleRescueZones(false);
            }

            if (Config.ExtendTime > 0)
            {
                ExtendRoundTime(Config.ExtendTime);
            }

            if (Config.DisallowWeaponPickup)
            {
                VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
            }

            RegisterListener<Listeners.OnTick>(OnTick);
        }

        private void OnTick()
        {
            if (CountPlayersAlive(CsTeam.CounterTerrorist) == 0 || CountPlayersAlive(CsTeam.Terrorist) == 0)
            {
                RemoveListener<Listeners.OnTick>(OnTick);
                _isActive = false;
                return;
            }

            if (!Config.Beam)
            {
                return;
            }

            foreach (CCSPlayerController player in GetAlivePlayers())
            {
                if (!IsPlayerValid(player) || player.Pawn.Value!.AbsOrigin == null)
                {
                    continue;
                }

                Vector currentPos = player.Pawn.Value.AbsOrigin + new Vector(0, 0, PlayerHeightOffset);
                bool isCT = player.Team == CsTeam.CounterTerrorist;
                ref Vector lastPos = ref isCT ? ref _lastCtPos : ref _lastTPos;
                Color beamColor = isCT ? Color.Blue : Color.Red;

                if (lastPos != Vector.Zero && GetVectorDistance(lastPos, currentPos) > Config.BeamLength)
                {
                    CreateBeam(currentPos, lastPos, beamColor, Config.BeamThickness, Config.BeamLifetime);
                }

                if (lastPos == Vector.Zero || GetVectorDistance(lastPos, currentPos) > Config.BeamLength)
                {
                    lastPos = currentPos;
                }
            }
        }

        private bool IsPlayerValid(CCSPlayerController player)
        {
            return player != null && player.Pawn != null && player.Pawn.IsValid && player.Pawn.Value != null;
        }
    }
}