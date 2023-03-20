using HarmonyLib;

using System;
using System.Linq;
using System.Reflection;

using Game;
using Game.Units;
using Ships;
using Munitions;

using Geoscream;

[HarmonyPatch]
internal class PatchShipControllerDoDamage
{
    [HarmonyTargetMethod]
    private static MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(ShipController)).Where<MethodInfo>((Func<MethodInfo, bool>)(method => method.ReturnType == typeof(HitResult))).Cast<MethodBase>().First<MethodBase>();

    private static bool Prefix(
      ShipController __instance,
      MunitionHitInfo hitInfo,
      IDamageDealer damager)
    {
        foreach (ShieldComponent shieldComponent in Enumerable.OfType<ShieldComponent>(__instance.Ship.Hull.AllComponents))
        {
            shieldComponent.CurrentDamageDealer = damager;
            shieldComponent.CurrentHitInfo = hitInfo;
        }
        return true;
    }
}
