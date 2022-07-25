using System;
using RoR2;
using UnityEngine;
using BepInEx;
using MonoMod.Cil;
using BepInEx.Configuration;

namespace R2API.Utils
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class ManualNetworkRegistrationAttribute : Attribute
    {
    }
}

namespace SacrificeTweaks
{
    [BepInPlugin("com.Moffein.SacrificeTweaks", "Sacrifice Tweaks", "1.1.1")]
    public class SacrificeTweaks : BaseUnityPlugin
    {
        public void Awake()
        {
            float baseDropChance = base.Config.Bind<float>(new ConfigDefinition("Sacrifice Tweaks", "Base Drop Chance"), 10f, new ConfigDescription("Base item drop chance.")).Value;
            float maxBaseDropChance = base.Config.Bind<float>(new ConfigDefinition("Sacrifice Tweaks", "Max Drop Chance"), 10f, new ConfigDescription("Maximum item drop chance when scaling.")).Value;

            float swarmDropChance = base.Config.Bind<float>(new ConfigDefinition("Sacrifice Tweaks", "Swarms Base Drop Chance"), 5f, new ConfigDescription("Base item drop chance when Swarms is enabled.")).Value;
            float maxSwarmDropChance = base.Config.Bind<float>(new ConfigDefinition("Sacrifice Tweaks", "Swarms Max Drop Chance"), 5f, new ConfigDescription("Maximum item drop chance when scaling while Swarms is enabled.")).Value;

            IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += (il) =>
            {
                ILCursor c = new ILCursor(il);

                //Change base drop chance
                c.GotoNext(MoveType.After,
                    x => x.MatchLdcR4(5f)
                    );
                c.EmitDelegate<Func<float, float>>(orig =>
                {
                    return RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef) ? swarmDropChance : baseDropChance;
                });

                //Clamp final drop chance
                c.GotoNext(
                    x => x.MatchStloc(0) //Called after GetExpAdjustedDropChancePercent
                    );
                c.EmitDelegate<Func<float, float>>(orig =>
                {
                    float finalDropChance = orig;
                    bool swarmsEnabled = RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.swarmsArtifactDef);

                    float baseChance = baseDropChance;
                    float maxChance = maxBaseDropChance;

                    if (swarmsEnabled)
                    {
                        baseChance = swarmDropChance;
                        maxChance = maxSwarmDropChance;
                    }

                    if (finalDropChance < baseChance)
                    {
                        finalDropChance = baseChance;
                    }

                    if (finalDropChance > maxChance)
                    {
                        finalDropChance = maxChance;
                    }

                    return finalDropChance;
                });
            };
        }
    }
}
