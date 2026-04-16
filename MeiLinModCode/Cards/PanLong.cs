using System.Collections.Generic;
using System.Linq;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class PanLong() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string GrowKey = "Grow";

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(5m, ValueProp.Move),
        new DynamicVar(GrowKey, 1m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var bonus = GetStrikePlayedCountThisCombat() * DynamicVars[GrowKey].BaseValue;
        var block = DynamicVars.Block.BaseValue + bonus;
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move | ValueProp.Unpowered, null, fast: true);
    }

    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        description.Add("CurrentBonus", IsMutable ? GetStrikePlayedCountThisCombat() * DynamicVars[GrowKey].BaseValue : 0m);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }

    private int GetStrikePlayedCountThisCombat()
    {
        if (!IsMutable)
            return 0;

        var history = CombatManager.Instance?.History?.CardPlaysFinished;
        if (history == null)
            return 0;

        return history.Count(e =>
            e.CardPlay.Card.Owner == Owner &&
            e.CardPlay.Card.Tags.Contains(CardTag.Strike));
    }
}
