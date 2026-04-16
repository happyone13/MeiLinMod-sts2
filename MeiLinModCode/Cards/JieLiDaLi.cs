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
public class JieLiDaLi() : MeiLinModCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await XiangzuLegacyApi.SetStance(Owner, XiangzuStance.Attack);

        await PowerCmd.Apply<BorrowForceShieldPower>(Owner.Creature, 1m, Owner.Creature, this);

        if (!IsUpgraded)
            return;

        var candidates = PileType.Draw.GetPile(Owner).Cards
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(BasicStrikeDefendHelper.IsBasicStrike)
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
