using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class GuJiChongShi() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
        MeiLinHoverTipFactory.Awakening
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner).Cards;
        var candidateCards = hand.Where(BasicStrikeDefendHelper.IsStrikeOrDefendCard).ToList();
        var candidates = candidateCards.Count;
        MainFile.Logger.Info($"[GuJiChongShi] candidates={candidates}");

        CardModel? target = null;
        if (candidates > 0)
        {
            target = (await CardSelectCmd.FromHand(
                    context: choiceContext,
                    player: Owner,
                    prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                    filter: BasicStrikeDefendHelper.IsStrikeOrDefendCard,
                    source: this))
                .FirstOrDefault();
        }

        MainFile.Logger.Info($"[GuJiChongShi] selected={(target?.Id.Entry ?? "null")}");

        if (target != null)
        {
            target.BaseReplayCount += 1;
            CardCmd.Preview(target);
        }

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        await XiangzuLegacyApi.ToggleAttackGuard(Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
