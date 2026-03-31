using System.Collections.Generic;
using BaseLib.Utils;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class GuiYi() : MeiLinModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        var legacy = Owner.Creature.GetPower<XiangzuLegacyPower>();
        if (legacy != null && IsUpgraded)
            await legacy.AddQiCounterProgress(3);

        await PowerCmd.Apply<GuiYiDualStancePower>(Owner.Creature, 1m, Owner.Creature, this);
        if (legacy != null)
            await legacy.RefreshFromStance();
    }

    protected override void OnUpgrade()
    {
    }
}


