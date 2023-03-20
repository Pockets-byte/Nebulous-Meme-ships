using HarmonyLib;
using Modding;

public class ModEntryPoint : IModEntryPoint
{
    public void PreLoad() { }

    public void PostLoad() {
        Harmony harmony = new Harmony("nebulous.shield.ramiel");
        harmony.PatchAll();
    }
}