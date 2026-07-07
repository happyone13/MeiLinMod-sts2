using System.Reflection;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using TestTheSpire;

namespace MeiLinMod.Tests;

[ModInitializer(nameof(Init))]
public static class Entry
{
    public static void Init()
    {
        CombatTestBootstrap.Initialize(Assembly.GetExecutingAssembly(), new CombatTestOptions
        {
            LogPrefix = "MeiLinMod.Tests"
        });

        Log.Info("[MeiLinMod.Tests] Mod initialized");
    }
}
