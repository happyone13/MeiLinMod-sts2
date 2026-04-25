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
public class XuLi() : MeiLinModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
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
        var candidates = PileType.Hand.GetPile(Owner).Cards.Count(BasicStrikeDefendHelper.IsStrikeOrDefendCard);
        MainFile.Logger.Info($"[XuLi] candidates={candidates}");

        var target = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: Owner,
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                filter: BasicStrikeDefendHelper.IsStrikeOrDefendCard,
                source: this))
            .FirstOrDefault();
        MainFile.Logger.Info($"[XuLi] selected={(target?.Id.Entry ?? "null")}");

        if (target != null)
        {
            var currentCost = target.EnergyCost.GetWithModifiers(CostModifiers.All);
            target.EnergyCost.AddThisCombat(1);
            target.BaseReplayCount += IsUpgraded ? 3 : 2;
            CardCmd.ApplyKeyword(target, CardKeyword.Exhaust);
            CardCmd.Preview(target);
        }

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

            await XiangzuLegacyApi.ToggleAttackGuard(Owner);
    }

    protected override void OnUpgrade()
    {
    }
}
