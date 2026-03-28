using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class TiShen() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string VigorKey = "Vigor";

    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.Any(c => c != this);
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar(VigorKey, 5m)];
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening, HoverTipFactory.FromPower<VigorPower>()];

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
        await PowerCmd.Apply<VigorPower>(Owner.Creature, DynamicVars[VigorKey].BaseValue, Owner.Creature, this);

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.EnterAttackStance();
    }

    protected override void OnUpgrade()
    {
        DynamicVars[VigorKey].UpgradeValueBy(2m);
    }
}
