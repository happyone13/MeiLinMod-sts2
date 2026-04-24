using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class YaZhi() : MeiLinModCard(-1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override bool HasEnergyCostX => true;

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var count = ResolveEnergyXValue();
        if (count > 0)
        {
            foreach (var enemy in CombatState.HittableEnemies)
                await PowerCmd.Apply<Powers.EmberPower>(enemy, count, Owner.Creature, this);
        }

        for (var i = 0; i < count; i++)
        {
            var strike = BasicStrikeDefendHelper.CreateBasicStrikeForPlayer(Owner, CombatState);
            if (strike == null)
                continue;

            strike.SetToFreeThisCombat();
            CardCmd.ApplyKeyword(strike, CardKeyword.Exhaust);
            await CardPileCmd.AddGeneratedCardToCombat(strike, PileType.Hand, Owner);

            if (IsUpgraded)
                CardCmd.Upgrade(strike);
        }
    }

    protected override void OnUpgrade()
    {
    }
}


