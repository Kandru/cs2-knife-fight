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

        private static PluginCapability<IPanoramaVoteManagerAPI> VoteAPI { get; } = new("panoramavotemanager:api");
        private IPanoramaVoteManagerAPI? _voteManager;
        private Vote? _vote;
        private (bool, bool) _isActive = (false, false);
        private (Vector, Vector) _lastPlayerPos = (Vector.Zero, Vector.Zero);

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
            }
            catch (Exception ex)
            {
                DebugPrint($"Failed to get PanoramaVoteManager API: {ex.Message}");
                // You might want to log this properly depending on your logging setup
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
            // disable during warmup
            if ((bool)GetGameRule("WarmupPeriod")!)
            {
                return HookResult.Continue;
            }
            // disable if already in knife fight or bomb is planted
            if (_isActive == (true, true)
                || IsBombPlanted())
            {
                return HookResult.Continue;
            }

            CCSPlayerController? victim = @event.Userid;
            if (victim == null)
            {
                return HookResult.Continue;
            }

            int aliveCT = CountPlayersAlive(CsTeam.CounterTerrorist) - (victim.Team == CsTeam.CounterTerrorist ? 1 : 0);
            int aliveT = CountPlayersAlive(CsTeam.Terrorist) - (victim.Team == CsTeam.Terrorist ? 1 : 0);
            if (!Config.Enabled
                || aliveCT != 1
                || aliveT != 1)
            {
                return HookResult.Continue;
            }

            if (_voteManager != null)
            {
                DebugPrint("vote-based knifefight");
                _vote = new(
                    sfui: Config.SfuiString,
                    text: new Dictionary<string, string> {
                        {"en", $"KNIFE FIGHT?"},
                        {"de", $"MESSERKAMPF?"},
                    },
                    time: Config.VoteTime,
                    team: -1,
                    playerIDs: GetAlivePlayerIds(),
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
                // TODO: not yet implemented
            }
            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _isActive = (false, false);
            if (_vote != null)
            {
                _ = (_voteManager?.RemoveVote(_vote));
                _vote = null;
            }
            // enable bombspots
            ToggleBombspots(true);
            // enable rescue zones
            ToggleRescueZones(true);
            return HookResult.Continue;
        }

        private HookResult OnWeaponCanAcquire(DynamicHook hook)
        {
            if (_isActive != (true, true))
            {
                return HookResult.Continue;
            }

            CCSWeaponBaseVData vdata = VirtualFunctions.GetCSWeaponDataFromKey.Invoke(-1, hook.GetParam<CEconItemView>(1).ItemDefinitionIndex.ToString());
            if (vdata == null)
            {
                return HookResult.Continue;
            }
            // disallow weapon pick up if not a knife
            return !vdata.Name.Contains("knife") ? HookResult.Stop : HookResult.Continue;
        }

        public void OnVoteResult(Vote vote, bool success)
        {
            if (_vote == null)
            {
                return;
            }

            DebugPrint($"Vote was {(success ? "successful" : "unsuccessful")} -> {vote._voters.Count} of {vote.PlayerIDs.Count} have voted");
            if (_isActive != (true, true) && success)
            {
                _isActive = (true, true);
                InitializeKnifeFight();
            }
        }

        private void InitializeKnifeFight()
        {
            if (_isActive != (true, true)
                || IsBombPlanted())
            {
                return;
            }
            // prepare each player
            foreach (CCSPlayerController player in GetAlivePlayers())
            {
                // make player glow
                if (Config.Glow)
                {
                    MakePlayerGlow(player);
                }
                // remove player weapons
                player.RemoveWeapons();
                // give knife to player
                Server.NextFrame(() =>
                {
                    if (player == null
                        || player.Pawn == null
                        || !player.Pawn.IsValid
                        || player.Pawn.Value == null)
                    {
                        return;
                    }
                    // give knife
                    foreach (string weapon in Config.Weapons)
                    {
                        _ = player.GiveNamedItem(weapon);
                    }
                    // set player health
                    player.Pawn.Value.MaxHealth = Config.Health;
                    player.Pawn.Value.Health = Config.Health;
                    Utilities.SetStateChanged(player.Pawn.Value, "CBaseEntity", "m_iHealth");
                });
                // send message to player
                player.PrintToCenterAlert(Localizer["knifefight.start"]);
            }
            // disable bombspots
            if (Config.DisableBombspots)
            {
                ToggleBombspots(false);
            }
            // disable rescue zones
            if (Config.DisableRescueZones)
            {
                ToggleRescueZones(false);
            }
            // set round time to 2 minutes
            if (Config.ExtendTime > 0)
            {
                ExtendRoundTime(Config.ExtendTime);
            }
            // disallow weapon pick up
            if (Config.DisallowWeaponPickup)
            {
                VirtualFunctions.CCSPlayer_ItemServices_CanAcquireFunc.Hook(OnWeaponCanAcquire, HookMode.Pre);
            }
            // run ontick event
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        private void OnTick()
        {
            int aliveCT = CountPlayersAlive(CsTeam.CounterTerrorist);
            int aliveT = CountPlayersAlive(CsTeam.Terrorist);
            if (aliveCT == 0 || aliveT == 0)
            {
                RemoveListener<Listeners.OnTick>(OnTick);
                _isActive = (false, false);
                return;
            }
            // update beam
            if (Config.Beam)
            {
                foreach (CCSPlayerController entry in GetAlivePlayers())
                {
                    if (entry.Pawn == null
                        || !entry.Pawn.IsValid
                        || entry.Pawn.Value == null
                        || entry.Pawn.Value.AbsOrigin == null)
                    {
                        continue;
                    }
                    // create beam for CT
                    if (entry.Team == CsTeam.CounterTerrorist
                        && _lastPlayerPos.Item1 != Vector.Zero
                        && GetVectorDistance(_lastPlayerPos.Item1, entry.Pawn.Value.AbsOrigin) > Config.BeamLength)
                    {
                        CreateBeam(
                            entry.Pawn.Value.AbsOrigin + new Vector(0, 0, 25),
                            _lastPlayerPos.Item1,
                            Color.Blue,
                            Config.BeamThickness,
                            Config.BeamLifetime
                        );
                    }
                    // create beam for T
                    if (entry.Team == CsTeam.Terrorist
                        && _lastPlayerPos.Item2 != Vector.Zero
                        && GetVectorDistance(_lastPlayerPos.Item2, entry.Pawn.Value.AbsOrigin) > Config.BeamLength)
                    {
                        CreateBeam(
                            entry.Pawn.Value.AbsOrigin + new Vector(0, 0, 25),
                            _lastPlayerPos.Item2,
                            Color.Red,
                            Config.BeamThickness,
                            Config.BeamLifetime
                        );
                    }
                    // update position
                    if (entry.Team == CsTeam.CounterTerrorist
                        && (_lastPlayerPos.Item1 == Vector.Zero
                            || GetVectorDistance(_lastPlayerPos.Item1, entry.Pawn.Value.AbsOrigin) > Config.BeamLength))
                    {
                        _lastPlayerPos.Item1 = new Vector(
                            entry.Pawn.Value.AbsOrigin.X,
                            entry.Pawn.Value.AbsOrigin.Y,
                            entry.Pawn.Value.AbsOrigin.Z + 25
                        );
                    }
                    else if (entry.Team == CsTeam.Terrorist
                        && (_lastPlayerPos.Item2 == Vector.Zero
                            || GetVectorDistance(_lastPlayerPos.Item2, entry.Pawn.Value.AbsOrigin) > Config.BeamLength))
                    {
                        _lastPlayerPos.Item2 = new Vector(
                            entry.Pawn.Value.AbsOrigin.X,
                            entry.Pawn.Value.AbsOrigin.Y,
                            entry.Pawn.Value.AbsOrigin.Z + 25
                        );
                    }
                }
            }
        }
    }
}