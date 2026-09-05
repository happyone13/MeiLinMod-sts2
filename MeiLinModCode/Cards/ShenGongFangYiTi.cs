using System.Collections.Generic;
using System.Linq;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Character;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Services;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Cards;

[Pool(typeof(MeiLinModCardPool))]
public class ShenGongFangYiTi() : MeiLinModCard(1, CardType.Skill, CardRarity.Ancient, TargetType.Self)
{
    public override string PortraitPath => IdPortraitPath;
    public override string CustomPortraitPath => IdBigPortraitPath;
    protected override string? CombatTimelineName => "u3_buff";

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!UltimateCinematicPlayedForCurrentPlay)
        {
            MeiLinAudioService.SuppressNextDefaultCastSfx(Owner);
            MeiLinAudioService.TryPlayCustomCardClip("attack_defense_unity", Owner);
        }

        var cardsToPlay = PileType.Hand.GetPile(Owner).Cards
            .Concat(PileType.Draw.GetPile(Owner).Cards)
            .Concat(PileType.Discard.GetPile(Owner).Cards)
            .Where(BasicStrikeDefendHelper.IsStrikeOrDefendCard)
            .ToList();

        foreach (var card in cardsToPlay)
        {
            await CardCmd.AutoPlay(choiceContext, card, ResolveTargetFor(card));
        }
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }

    private Creature? ResolveTargetFor(CardModel card)
    {
        if (card.TargetType != TargetType.AnyEnemy)
            return null;

        var combatState = CombatState;
        if (combatState == null)
            return null;

        var enemies = combatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return null;

        return Owner.RunState.Rng.CombatTargets.NextItem(enemies);
    }
}
