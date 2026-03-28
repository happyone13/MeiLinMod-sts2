using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class XiaoLi() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.AllEnemies)
{
    private const string ProgressKey = "Progress";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Weak", 2m),
        new DynamicVar(ProgressKey, 2m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var enemy in CombatState.HittableEnemies)
            await PowerCmd.Apply<WeakPower>(enemy, DynamicVars["Weak"].BaseValue, Owner.Creature, this);

        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.AddQiCounterProgress(DynamicVars[ProgressKey].IntValue);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await CardPileCmd.Draw(choiceContext, 1m, Owner);
        if (legacy != null)
            await legacy.EnterGuardStance();
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(1m);
    }
}



