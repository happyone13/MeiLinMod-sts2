using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
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

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.Static(StaticHoverTip.ReplayStatic),
        MeiLinHoverTipFactory.Awakening
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var candidates = PileType.Hand.GetPile(Owner).Cards.Count(BasicStrikeDefendHelper.IsBasicStrikeOrDefend);
        MainFile.Logger.Info($"[GuJiChongShi] candidates={candidates}");

        var target = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: Owner,
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: BasicStrikeDefendHelper.IsBasicStrikeOrDefend,
                source: this))
            .FirstOrDefault();
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
