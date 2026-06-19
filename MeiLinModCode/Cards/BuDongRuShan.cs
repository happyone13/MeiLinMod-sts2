using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.HoverTips;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(NoneCardPool))]
public class BuDongRuShan() : MeiLinModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override bool ShouldGlowGoldInternal => AwakeningHelper.CanAwakenNow(this);
    protected override IEnumerable<IHoverTip> ExtraHoverTips => [MeiLinHoverTipFactory.Awakening];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NextBasicDefendFreePower>(new BlockingPlayerChoiceContext(), Owner.Creature, 1m, Owner.Creature, this);

        if (AwakeningHelper.IsAwakened(cardPlay))
        {
            await XiangzuLegacyApi.SetStance(Owner, XiangzuStance.Guard);
        }

        if (!IsUpgraded)
            return;

        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(BasicStrikeDefendHelper.IsDefendCard)
            .ToList();
        if (candidates.Count == 0)
            return;

        var selected = Owner.RunState.Rng.CombatTargets.NextItem(candidates);
        if (selected == null)
            return;
        await CardPileCmd.Add(selected, PileType.Hand);
    }

    protected override void OnUpgrade()
    {
    }
}
