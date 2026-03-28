using BaseLib.Abstracts;
using Godot;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MeiLinMod.MeiLinModCode.Relics;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinMod : PlaceholderCharacterModel
{
    public const string CharacterId = "MeiLinMod";

    public static readonly Color Color = new("FFC0CB");

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 75;

    public override IEnumerable<CardModel> StartingDeck =>
    [
        ModelDb.Card<AttackDefenseUnity>(),
        ModelDb.Card<FireDragonGem>(),
        ModelDb.Card<StrikeMeilin>(),
        ModelDb.Card<StrikeMeilin>(),
        ModelDb.Card<StrikeMeilin>(),
        ModelDb.Card<StrikeMeilin>(),
        ModelDb.Card<StrikeMeilin>(),
        ModelDb.Card<DefendMeilin>(),
        ModelDb.Card<DefendMeilin>(),
        ModelDb.Card<DefendMeilin>(),
        ModelDb.Card<DefendMeilin>(),
        ModelDb.Card<DefendMeilin>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        ModelDb.Relic<XiangzuLegacyRelic>()
    ];

    public override CardPoolModel CardPool => ModelDb.CardPool<MeiLinModCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MeiLinModRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MeiLinModPotionPool>();

    public override string CustomIconTexturePath => "character_icon_meilin_name.png".CharacterUiPath();
    public override string CustomCharacterSelectIconPath => "char_select_char_meilin.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_meilin_name.png".CharacterUiPath();
    public override Color EnergyLabelOutlineColor => Color.Color8(255, 192, 203);
    public override string CustomIconPath => "res://MeiLinMod/scenes/meilin_icon.tscn";
    public override string CustomVisualPath => "res://MeiLinMod/scenes/meilin_character.tscn";
    public override string CustomRestSiteAnimPath => "res://MeiLinMod/scenes/meilin_character_camp.tscn";
    public override string CustomMerchantAnimPath => "res://MeiLinMod/scenes/merchant/characters/meilinmod_merchant.tscn";
    public override string CustomCharacterSelectBg => "res://MeiLinMod/scenes/meilin_bg.tscn";
    public override string CharacterTransitionSfx => "event:/sfx/ui/wipe_ironclad";
    // 多人模式-手指。
    public override string CustomArmPointingTexturePath => null;
    // 多人模式剪刀石头布-石头。
    // public override string CustomArmRockTexturePath => null;
    // 多人模式剪刀石头布-布。
    // public override string CustomArmPaperTexturePath => null;
    // 多人模式剪刀石头布-剪刀。
    // public override string CustomArmScissorsTexturePath => null;
    // 攻击音效
    // public override string CustomAttackSfx => null;
    // 施法音效
    // public override string CustomCastSfx => null;
    // 死亡音效
    // public override string CustomDeathSfx => null;
    // 角色选择音效
    // public override string CharacterSelectSfx => null;
    public override List<string> GetArchitectAttackVfx() =>
    [
        "vfx/vfx_attack_blunt",
        "vfx/vfx_heavy_blunt",
        "vfx/vfx_attack_slash",
        "vfx/vfx_bloody_impact",
        "vfx/vfx_rock_shatter"
    ];

    public override CreatureAnimator GenerateAnimator(MegaSprite controller)
    {
        AnimState idle = new("b_idle", isLooping: true);
        AnimState attack = new("attack_play1");
        AnimState cast = new("buff_play");
        AnimState hit = new("hit");
        AnimState dead = new("death");
        AnimState relaxed = new("camping", isLooping: true);

        attack.NextState = idle;
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
