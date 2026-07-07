using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Patches;
using STS2RitsuLib.Patching.Models;
using TestTheSpire;
using Xunit;
using MeiLinCharacter = MeiLinMod.MeiLinModCode.Character.MeiLinMod;

namespace MeiLinMod.Tests;

public sealed class OptionalContentPatchTests : CombatTestSuite
{
    protected override void ConfigureBattle(CombatTestBattleBuilder battle)
    {
        battle
            .Player<MeiLinCharacter>()
            .AddEnemy<BigDummy>()
            .WithSeed("meilinmod-optional-content-patches");
    }

    [Fact]
    public async Task Optional_content_patches_stay_non_critical_with_stable_descriptions()
    {
        await InitializeBattle();

        Type[] contentPatchTypes =
        [
            typeof(ArchaicToothAfterObtainedMeiLinPatch),
            typeof(ArchaicToothSetupForPlayerMeiLinPatch),
            typeof(ColorfulPhilosophersMeiLinPatch),
            typeof(DustyTomeAfterObtainedMeiLinPatch),
            typeof(DustyTomeSetupForPlayerMeiLinPatch),
            typeof(HuoYongYuXiaCiAfterCombatPatch),
            typeof(OrobasSeaGlassMeiLinPatch),
            typeof(PrismaticGemMeiLinPatch),
            typeof(TouchOfOrobasMeiLinPatch)
        ];

        foreach (var patchType in contentPatchTypes)
        {
            Assert.False(ReadStatic<bool>(patchType, nameof(IPatchMethod.IsCritical)));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.PatchId))));
            Assert.False(string.IsNullOrWhiteSpace(ReadStatic<string>(patchType, nameof(IPatchMethod.Description))));
            Assert.NotEmpty(InvokeStatic<ModPatchTarget[]>(patchType, nameof(IPatchMethod.GetTargets)));
        }
    }

    [Fact]
    public async Task Dusty_tome_candidate_filter_excludes_meilin_archaic_tooth_target()
    {
        await InitializeBattle();

        Assert.False(IsDustyTomeCandidate(ModelDb.Card<ShenGongFangYiTi>()));
        Assert.False(IsDustyTomeCandidate(ModelDb.Card<AttackDefenseUnity>()));
        Assert.True(IsDustyTomeCandidate(ModelDb.Card<ZuiZhongAoYiYanLongJiangLin>()));
    }

    private async Task InitializeBattle()
    {
        var defend = await AddToHand<DefendMeilin>();
        await Play(defend);
    }

    private static bool IsDustyTomeCandidate(CardModel card)
    {
        var helperType = typeof(ArchaicToothSetupForPlayerMeiLinPatch).Assembly.GetType(
            "MeiLinMod.MeiLinModCode.Patches.AncientRelicMeiLinPatch");
        Assert.NotNull(helperType);

        var method = helperType.GetMethod("IsDustyTomeCandidate", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        return (bool)(method.Invoke(null, [card])
                      ?? throw new InvalidOperationException("IsDustyTomeCandidate returned null."));
    }

    private static T ReadStatic<T>(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(null));
    }

    private static T InvokeStatic<T>(Type type, string methodName)
    {
        var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        return Assert.IsType<T>(method.Invoke(null, null));
    }
}
