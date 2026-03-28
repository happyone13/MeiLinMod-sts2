using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class WuDao() : MeiLinModCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private const string VigorKey = "Vigor";

    public override bool GainsBlock => true;
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(c => c != this);
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move),
        new DynamicVar(VigorKey, 3m)
    ];
    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        EnergyHoverTip,
        MeiLinHoverTipFactory.Awakening,
        HoverTipFactory.FromPower<VigorPower>()
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var selected = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: Owner,
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: c => c != this,
                source: this))
            .FirstOrDefault();

        if (selected == null)
            return;

        await CardPileCmd.Add(selected, PileType.Exhaust);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await PlayerCmd.LoseEnergy(2, Owner);
        var handCards = PileType.Hand.GetPile(Owner).Cards.ToList();
        foreach (var card in handCards)
            await CardPileCmd.Add(card, PileType.Exhaust);

        if (handCards.Count > 0)
            await PowerCmd.Apply<VigorPower>(Owner.Creature, handCards.Count * DynamicVars[VigorKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
    }
}
