using HarmonyLib;

using System.Linq;

using Ships;
using Geoscream;

[HarmonyPatch(typeof(BaseHull), "WouldArmorHitPenetrate")]
public class PatchBaseHullWouldArmorHitPenetrate
{
    private static bool Prefix(BaseHull __instance, ref bool __state, ref float damage)
    {
        __state = false;
        bool flag = false;
        ShieldComponent shieldComponent1 = (ShieldComponent)null;
        foreach (ShieldComponent shieldComponent2 in Enumerable.OfType<ShieldComponent>(__instance.AllComponents))
        {
            if (shieldComponent2.IsFunctional && !shieldComponent2.CycleActive)
            {
                flag = true;
                if (shieldComponent1 == null || (double)shieldComponent2.BurstPercent < (double)shieldComponent1.BurstPercent)
                    shieldComponent1 = shieldComponent2;
            }
        }
        if (flag)
        {
            if ((double)shieldComponent1.BurstPercent <= 1.0 - (double)shieldComponent1.ShieldLeakFraction)
            {
                damage = 0.0f;
                __state = true;
            }
            else
                damage *= (float)(((double)shieldComponent1.BurstPercent - (1.0 - (double)shieldComponent1.ShieldLeakFraction)) * (1.0 / ((double)shieldComponent1.ShieldLeakFraction + 1.0000000116860974E-07)));
            shieldComponent1.TakeDamage();
        }
        return true;
    }

    private static void Postfix(ref bool __result, ref bool __state, ref float damageScaling, ref bool ricochet)
    {
        if (!__state)
            return;
        damageScaling = 0.0f;
        ricochet = false;
        __result = false;
    }
}
