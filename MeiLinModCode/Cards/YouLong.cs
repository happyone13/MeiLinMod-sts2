using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class YouLong() : MeiLinModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    private const string ValueKey = "Value";

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar(ValueKey, 2m)
    ];

    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayPowerCastAnim();
        await PowerCmd.Apply<YouLongPower>(new BlockingPlayerChoiceContext(), Owner.Creature, DynamicVars[ValueKey].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars[ValueKey].UpgradeValueBy(1m);
    }
}


