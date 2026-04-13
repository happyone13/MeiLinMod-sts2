using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class PanLong() : MeiLinModCard(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    private const string GrowKey = "Grow";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(3m, ValueProp.Move),
        new BlockVar(3m, ValueProp.Move),
        new DynamicVar(GrowKey, 1m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var usedCount = CombatManager.Instance.History.CardPlaysFinished.Count(e =>
            e.CardPlay.Card.Owner == Owner &&
            BasicStrikeDefendHelper.IsBasicStrikeOrDefend(e.CardPlay.Card));
        var bonus = usedCount * DynamicVars[GrowKey].BaseValue;
        var damage = DynamicVars.Damage.BaseValue + bonus;
        var block = DynamicVars.Block.BaseValue + bonus;

        if (cardPlay.Target != null)
        {
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move | ValueProp.Unpowered, null, fast: true);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        if (!IsMutable)
        {
            description.Add("CurrentBonus", 0m);
            return;
        }

        var usedCount = CombatManager.Instance?.History?.CardPlaysFinished.Count(e =>
            e.CardPlay.Card.Owner == Owner &&
            BasicStrikeDefendHelper.IsBasicStrikeOrDefend(e.CardPlay.Card)) ?? 0;
        description.Add("CurrentBonus", usedCount * DynamicVars[GrowKey].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars[GrowKey].UpgradeValueBy(1m);
    }
}
