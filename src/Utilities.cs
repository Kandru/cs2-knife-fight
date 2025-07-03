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
                .Count(player => player.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE && !player.IsHLTV && player.Team == team);
        }

        private static List<CCSPlayerController> GetAlivePlayers()
        {
            return [.. Utilities.GetPlayers()
                .Where(static player => player.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE && !player.IsHLTV)];
        }

        private static List<int> GetAlivePlayerIds()
        {
            return [.. Utilities.GetPlayers()
                .Where(static player => player.Pawn?.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE && !player.IsHLTV && !player.IsBot)
                .Select(static player => player.UserId!.Value)];
        }

        public static void ExtendRoundTime(int additionalSeconds)
        {
            CCSGameRulesProxy gameRules = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").First();
            if (gameRules == null)
            {
                return;
            }

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
                modelGlow.Glow.GlowColorOverride = playerPawn.TeamNum == (int)CsTeam.Terrorist ? Color.Red : Color.Blue;

                modelGlow.Spawnflags = 256u;
                modelGlow.RenderMode = RenderMode_t.kRenderGlow;
                modelGlow.Glow.GlowRange = 5000;
                modelGlow.Glow.GlowTeam = -1;
                modelGlow.Glow.GlowType = 3;
                modelGlow.Glow.GlowRangeMin = 30;
                DebugPrint($"Made {player.PlayerName} glowing");
            }
        }

        private static void ToggleBombspots(bool enable = true)
        {
            IEnumerable<CBombTarget> bombSites = Utilities.FindAllEntitiesByDesignerName<CBombTarget>("func_bomb_target");
            if (bombSites == null)
            {
                return;
            }

            foreach (CBombTarget bombSite in bombSites)
            {
                bombSite.Disabled = !enable;
            }
        }

        private static void ToggleRescueZones(bool enable = true)
        {
            IEnumerable<CHostageRescueZone> rescueZones = Utilities.FindAllEntitiesByDesignerName<CHostageRescueZone>("func_hostage_rescue");
            if (rescueZones == null)
            {
                return;
            }

            foreach (CHostageRescueZone rescueZone in rescueZones)
            {
                rescueZone.Disabled = !enable;
            }
        }

        private static bool IsBombPlanted()
        {
            IEnumerable<CPlantedC4> planted = Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4");
            return planted != null && planted.Any();
        }

        private static object? GetGameRule(string rule)
        {
            IEnumerable<CCSGameRulesProxy> ents = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
            foreach (CCSGameRulesProxy ent in ents)
            {
                CCSGameRules? gameRules = ent.GameRules;
                if (gameRules == null)
                {
                    continue;
                }

                System.Reflection.PropertyInfo? property = gameRules.GetType().GetProperty(rule);
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
            return MathF.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
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
            {
                _ = AddTimer(timeout, () =>
                {
                    if (beam != null && beam.IsValid)
                    {
                        beam.Remove();
                    }
                });
            }
        }
    }
}