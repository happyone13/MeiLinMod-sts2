using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class WuDao() : MeiLinModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var poolCards = Owner.Character.CardPool
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Type == CardType.Power && c.Id != Id)
            .ToList();
        if (poolCards.Count == 0)
            return;

        var pickedCanonical = Owner.RunState.Rng.CombatCardSelection.NextItem(poolCards);
        var created = CombatState.CreateCard(pickedCanonical, Owner);
        created.EnergyCost.AddThisTurn(-1, reduceOnly: true);
        if (IsUpgraded)
            CardCmd.Upgrade(created);
        await CardPileCmd.Add(created, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
    }
}
