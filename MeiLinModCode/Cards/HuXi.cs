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
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class HuXi() : MeiLinModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    private const string RetainKey = "Retain";

    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(RetainKey, 1m)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips =>
    [
        HoverTipFactory.FromKeyword(CardKeyword.Retain),
        MeiLinHoverTipFactory.Awakening
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var freeCount = IsUpgraded ? 2m : 1m;
        await PowerCmd.Apply<NextBasicStrikeDefendFreePower>(Owner.Creature, freeCount, Owner.Creature, this);

        var candidates = PileType.Hand.GetPile(Owner).Cards.Count(c => !c.ShouldRetainThisTurn);
        MainFile.Logger.Info($"[HuXi] retain candidates={candidates}");

        var retainCards = (await CardSelectCmd.FromHand(
                context: choiceContext,
                player: Owner,
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 0, DynamicVars[RetainKey].IntValue),
                filter: c => !c.ShouldRetainThisTurn,
                source: this))
            .ToList();
        MainFile.Logger.Info($"[HuXi] selected count={retainCards.Count}");

        foreach (var card in retainCards)
            card.GiveSingleTurnRetain();

        if (!AwakeningHelper.IsAwakened(cardPlay))
            return;

        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null)
            await legacy.EnterGuardStance();
    }

    protected override void OnUpgrade()
    {
    }
}


