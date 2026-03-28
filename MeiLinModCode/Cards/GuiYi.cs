using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class GuiYi() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string ProgressKey = "Progress";

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(ProgressKey, 3m), new CardsVar(2)];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.AddQiCounterProgress(DynamicVars[ProgressKey].IntValue);

        var hasStance = Owner.Creature.HasPower<StanceGongPower>() || Owner.Creature.HasPower<StanceYuPower>();
        if (!hasStance || legacy == null)
            return;

        await legacy.EnterNeutralStance();
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ProgressKey].UpgradeValueBy(2m);
    }
}


