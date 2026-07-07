using System.Reflection;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Potions;
using MeiLinMod.MeiLinModCode.Powers;
using MeiLinMod.MeiLinModCode.Relics;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace MeiLinMod.MeiLinModCode.Migration;

internal static class MeiLinRitsuContentRegistration
{
    private static readonly MethodInfo ApplyFixedPublicEntryForModelMethod =
        typeof(ModContentRegistry).GetMethod(
            "ApplyFixedPublicEntryForModel",
            BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingMethodException(
            typeof(ModContentRegistry).FullName,
            "ApplyFixedPublicEntryForModel");

    public static void Register(Assembly assembly)
    {
        var registry = ModContentRegistry.For(MainFile.ModId);

        ApplyLegacyPublicEntry(registry, typeof(Character.MeiLinMod));
        registry.RegisterCharacter<Character.MeiLinMod>();

        foreach (var type in GetConcreteTypes(assembly))
        {
            if (typeof(MeiLinModCard).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModCardPool);
                registry.RegisterCard(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(MeiLinModRelic).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModRelicPool);
                registry.RegisterRelic(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(MeiLinModPotion).IsAssignableFrom(type))
            {
                var poolType = type.GetCustomAttribute<PoolAttribute>(inherit: true)?.PoolType ?? typeof(MeiLinModPotionPool);
                registry.RegisterPotion(poolType, type, LegacyPublicEntry(type));
                continue;
            }

            if (typeof(MeiLinModPower).IsAssignableFrom(type))
            {
                ApplyLegacyPublicEntry(registry, type);
                registry.RegisterPower(type);
            }
        }
    }

    private static IEnumerable<Type> GetConcreteTypes(Assembly assembly)
    {
        return assembly
            .GetTypes()
            .Where(static type => type is { IsAbstract: false, IsInterface: false });
    }

    private static ModelPublicEntryOptions LegacyPublicEntry(Type type)
    {
        var stem = ModContentRegistry.NormalizePublicStem(type.Name);
        return ModelPublicEntryOptions.FromFullPublicEntry($"MEILINMOD_{stem}");
    }

    private static void ApplyLegacyPublicEntry(ModContentRegistry registry, Type type)
    {
        ApplyFixedPublicEntryForModelMethod.Invoke(registry, [type, LegacyPublicEntry(type)]);
    }
}
