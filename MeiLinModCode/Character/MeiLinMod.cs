using Godot;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;
using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MeiLinMod.MeiLinModCode.Relics;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinMod : ModCharacterTemplate<MeiLinModCardPool, MeiLinModRelicPool, MeiLinModPotionPool>
{
    public const string CharacterId = "MeiLinMod";

    public static readonly Color Color = new("FFC0CB");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;
    public override int StartingGold => 99;
    public override float AttackAnimDelay => 0.15f;
    public override float CastAnimDelay => 0.25f;

    [Obsolete("Legacy starter hook", false)]
    protected override IEnumerable<StartingDeckEntry> StartingDeckEntries =>
    [
        StartingDeckEntry.Of<AttackDefenseUnity>(),
        StartingDeckEntry.Of<FireDragonGem>(),
        StartingDeckEntry.Of<StrikeMeilin>(4),
        StartingDeckEntry.Of<DefendMeilin>(4)
    ];

    [Obsolete("Legacy starter hook", false)]
    protected override IEnumerable<Type> StartingRelicTypes => [typeof(XiangzuLegacyRelic)];


    public override string CustomIconTexturePath => "character_icon_meilin_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_meilin.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_meilin_name.png".CharacterUiPath();
    public override Color EnergyLabelOutlineColor => Color.Color8(255, 100, 100);
    public override string CustomIconPath => "res://MeiLinMod/scenes/meilin_icon.tscn";
    public override string? CustomVisualsPath => "res://MeiLinMod/scenes/meilin_character.tscn";
    public override string CustomRestSiteAnimPath => "res://MeiLinMod/scenes/meilin_character_camp.tscn";
    public override string CustomMerchantAnimPath => "res://MeiLinMod/scenes/merchant/characters/meilinmod_merchant.tscn";
    public override string? CustomCharacterSelectBgPath => "res://MeiLinMod/scenes/meilin_bg.tscn";
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
    // 多人模式-手指。
    public override string CustomArmPointingTexturePath => "multiplayer_hand_meilin_point.png".CharacterUiPath();
    // 多人模式剪刀石头布-石头。 
    public override string CustomArmRockTexturePath => "multiplayer_hand_meilin_rock.png".CharacterUiPath();
    // 多人模式剪刀石头布-布。
    public override string CustomArmPaperTexturePath => "multiplayer_hand_meilin_paper.png".CharacterUiPath();
    // 多人模式剪刀石头布-剪刀。
    public override string CustomArmScissorsTexturePath => "multiplayer_hand_meilin_scissors.png".CharacterUiPath();
    public override string CustomAttackSfx => "meilin_attack";
    public override string CustomCastSfx => "meilin_cast";
    public override string CustomDeathSfx => "meilin_die";
    public override string? CustomCharacterSelectSfx => "meilin_select";

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(CustomVisualsPath!);
    }

    public override bool RequiresEpochAndTimeline => false;

    public override List<string> GetArchitectAttackVfx() =>
    [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
    ];

    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller)
    {
        AnimState idle = new("idle", isLooping: true);
        AnimState attackEnd = new("attack_end") { NextState = idle };
        AnimState attack = new("attack_play1") { NextState = attackEnd };
        AnimState cast = new("buff_play");
        AnimState hit = new("hit");
        AnimState dead = new("death");
        AnimState relaxed = new("camping", isLooping: true);

        cast.NextState = idle;
        hit.NextState = idle;

        CreatureAnimator animator = new(idle, controller);
        animator.AddAnyState("Idle", idle);
        animator.AddAnyState("Dead", dead);
        animator.AddAnyState("Hit", hit);
        animator.AddAnyState("Attack", attack);
        animator.AddAnyState("Cast", cast);
        animator.AddAnyState("Relaxed", relaxed);
        animator.AddAnyState("Revive", idle);
        return animator;
    }

}
