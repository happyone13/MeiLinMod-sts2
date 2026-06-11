using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class TongQiao() : MeiLinModCard(3, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();

        var targetMode = IsUpgraded ? 2m : 1m;
        var existing = Owner.Creature.GetPower<TongQiaoPower>();
        if (existing != null)
        {
            var currentMode = existing.Amount >= 2m ? 2m : 1m;
            if (currentMode == targetMode)
                return;

            await PowerCmd.Remove<TongQiaoPower>(Owner.Creature);
        }

        await PowerCmd.Apply<TongQiaoPower>(new BlockingPlayerChoiceContext(), Owner.Creature, targetMode, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}


