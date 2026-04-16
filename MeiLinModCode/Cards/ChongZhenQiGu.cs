using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ChongZhenQiGu() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string ProgressKey = "Progress";
    protected override bool IsPlayable => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(ProgressKey, 2m)];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        MeiLinHoverTipFactory.Qi,
        MeiLinHoverTipFactory.QiConsume
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if ((Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) < 1m)
            return;

        await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);

        var discard = PileType.Discard.GetPile(Owner).Cards.ToList();
        if (discard.Count == 0)
            return;

        var selected = (await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                discard,
                Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1)))
            .FirstOrDefault();

        if (selected != null)
        {
            selected.EnergyCost.AddThisCombat(-1);
            await CardPileCmd.Add(selected, PileType.Hand);
        }

        if (IsUpgraded)
            await QiCounterPower.AddProgress(Owner.Creature, DynamicVars[ProgressKey].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}
