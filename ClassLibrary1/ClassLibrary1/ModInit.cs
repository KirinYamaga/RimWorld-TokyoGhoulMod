using Verse;
using HarmonyLib;
using System.Reflection;

namespace TokyoGhoulMod
{
    [StaticConstructorOnStartup]
    public static class TokyoGhoul_PostInit
    {
        static TokyoGhoul_PostInit()
        {
            var harmony = new Harmony("kirinyamaga.tokyoghoul");
            harmony.PatchAll();
            Log.Message("[Tokyo Ghoul] Мод успешно инициализирован.");
        }
    }
}