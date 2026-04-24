using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class BianHuaMoCe() : MeiLinModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    private const string VulnerableKey = "Vulnerable";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(VulnerableKey, 2m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null)
            return;

        await PowerCmd.Apply<VulnerablePower>(cardPlay.Target, DynamicVars[VulnerableKey].BaseValue, Owner.Creature, this);

        var strike = RandomStrikeHelper.CreateRandomNonBasicStrike(Owner, CombatState, IsUpgraded, true);
        if (strike != null)
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
    }
}
