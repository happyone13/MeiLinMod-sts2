using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ChongZhenQiGu() : MeiLinModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool IsPlayable => (Owner.Creature.GetPower<QiPower>()?.Amount ?? 0m) >= 1m;

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner.Creature.GetPower<QiPower>()?.Amount < 1m)
            return;

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
            await PowerCmd.Apply<QiPower>(Owner.Creature, -1m, Owner.Creature, this);
            if (IsUpgraded)
                selected.EnergyCost.SetUntilPlayed(0);
            await CardPileCmd.Add(selected, PileType.Hand);
        }
    }

    protected override void OnUpgrade()
    {
    }
}
