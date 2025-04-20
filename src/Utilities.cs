using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace KnifeFight
{
    public partial class KnifeFight
    {
        private void DebugPrint(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
            }
        }

        private static int CountPlayersAlive(CsTeam team)
        {
            return Utilities.GetPlayers()
                .Where(player => player.PawnIsAlive && !player.IsHLTV && player.Team == team)
                .Count();
        }

        private static List<CCSPlayerController> GetAlivePlayers()
        {
            return [.. Utilities.GetPlayers()
                .Where(player => player.PawnIsAlive && !player.IsHLTV)];
        }

        private static List<int> GetAlivePlayerIds()
        {
            return [.. Utilities.GetPlayers()
                .Where(player => player.PawnIsAlive && !player.IsHLTV && !player.IsBot && player.UserId.HasValue)
                .Select(player => player.UserId!.Value)];
        }

        public static void ExtendRoundTime(int additionalSeconds)
        {
            var gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
            if (gameRules == null)
                return;
            gameRules.GameRules!.RoundTime += additionalSeconds;
            Utilities.SetStateChanged(gameRules, "CCSGameRules", "m_iRoundTime");
        }

        public void MakePlayerGlow(CCSPlayerController player)
        {
            CCSPlayerPawn? playerPawn = player.PlayerPawn.Value;
            CDynamicProp? modelRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            CDynamicProp? modelGlow = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (playerPawn != null && modelGlow != null && modelRelay != null)
            {
                string modelName = playerPawn.CBodyComponent!.SceneNode!.GetSkeletonInstance().ModelState.ModelName;
                modelRelay.Spawnflags = 256u;
                modelRelay.RenderMode = RenderMode_t.kRenderNone;
                modelRelay.SetModel(modelName);
                modelRelay.AcceptInput("FollowEntity", playerPawn, modelRelay, "!activator");
                modelRelay.DispatchSpawn();
                modelGlow.SetModel(modelName);
                modelGlow.AcceptInput("FollowEntity", modelRelay, modelGlow, "!activator");
                modelGlow.DispatchSpawn();
                modelGlow.Render = Color.FromArgb(1, 255, 255, 255);
                if (playerPawn.TeamNum == (int)CsTeam.Terrorist) modelGlow.Glow.GlowColorOverride = Color.Red;
                else
                    modelGlow.Glow.GlowColorOverride = Color.Blue;
                modelGlow.Spawnflags = 256u;
                modelGlow.RenderMode = RenderMode_t.kRenderGlow;
                modelGlow.Glow.GlowRange = 5000;
                modelGlow.Glow.GlowTeam = -1;
                modelGlow.Glow.GlowType = 3;
                modelGlow.Glow.GlowRangeMin = 30;
                DebugPrint($"Made {player.PlayerName} glowing");
            }
        }

        private void ToggleBombspots(bool enable = true)
        {
            var bombSites = Utilities.FindAllEntitiesByDesignerName<CBombTarget>("func_bomb_target");
            if (bombSites == null) return;
            foreach (var bombSite in bombSites)
            {
                bombSite.Disabled = !enable;
            }
        }

        private void ToggleRescueZones(bool enable = true)
        {
            var rescueZones = Utilities.FindAllEntitiesByDesignerName<CHostageRescueZone>("func_hostage_rescue");
            if (rescueZones == null) return;
            foreach (var rescueZone in rescueZones)
            {
                rescueZone.Disabled = !enable;
            }
        }

        private bool IsBombPlanted()
        {
            var planted = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
            if (planted == null) return false;
            return planted.Any();
        }

        private static object? GetGameRule(string rule)
        {
            var ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (var ent in ents)
            {
                var gameRules = ent.GameRules;
                if (gameRules == null) continue;

                var property = gameRules.GetType().GetProperty(rule);
                if (property != null && property.CanRead)
                {
                    return property.GetValue(gameRules);
                }
            }
            return null;
        }

        private static float GetVectorDistance(Vector a, Vector b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private void CreateBeam(Vector startOrigin, Vector endOrigin, Color? color = null, float width = 1f, float timeout = 2f)
        {
            color ??= Color.White;
            CEnvBeam beam = Utilities.CreateEntityByName<CEnvBeam>("env_beam")!;
            beam.Width = width;
            beam.Render = color.Value;
            beam.SetModel("materials/sprites/laserbeam.vtex");
            beam.Teleport(startOrigin);
            beam.EndPos.X = endOrigin.X;
            beam.EndPos.Y = endOrigin.Y;
            beam.EndPos.Z = endOrigin.Z;
            Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");
            if (timeout > 0)
                AddTimer(timeout, () =>
                {
                    if (beam != null && beam.IsValid)
                        beam.Remove();
                });
        }
    }
}