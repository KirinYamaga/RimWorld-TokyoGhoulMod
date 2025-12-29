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
            // Создаем экземпляр Harmony с уникальным ID
            var harmony = new Harmony("kirinyamaga.tokyoghoul");

            // Выполняем патчи
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("[Tokyo Ghoul] Harmony patches applied successfully.");
        }
    }
}