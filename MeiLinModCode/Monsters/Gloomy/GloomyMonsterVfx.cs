using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Services;
using MeiLinMod.MeiLinModCode.Vfx;

namespace MeiLinMod.MeiLinModCode.Monsters.Gloomy;

internal static class GloomyMonsterVfx
{
    private const string SceneRoot = "res://MeiLinMod/scenes/vfx";
    private const string SoundRoot = "res://MeiLinMod/sound/gloomy_monsters";

    public const string PrimeAttackPlay1 = $"{SceneRoot}/gloomy_prime_normal_attack_play/gloomy_prime_normal_attack_play1.tscn";
    public const string PrimeAttackPlay2 = $"{SceneRoot}/gloomy_prime_normal_attack_play/gloomy_prime_normal_attack_play2.tscn";
    public const string PrimeBuff = $"{SceneRoot}/gloomy_prime_normal_buff_1/gloomy_prime_normal_buff_1.tscn";
    public const string PrimeUnique = $"{SceneRoot}/gloomy_prime_normal_unique_1/gloomy_prime_normal_unique_1.tscn";
    public const string PrimeUniqueTarget = $"{SceneRoot}/gloomy_prime_normal_unique_1/gloomy_prime_normal_unique_1_target.tscn";
    public const string BeastBuff = $"{SceneRoot}/gloomy_beast_normal_buff_1/gloomy_beast_normal_buff_1.tscn";
    public const string BeastAttack01 = $"{SceneRoot}/gloomy_beast_normal_m_attack_01/gloomy_beast_normal_m_attack_01.tscn";
    public const string BeastAttack02 = $"{SceneRoot}/gloomy_beast_normal_m_attack_02/gloomy_beast_normal_m_attack_02.tscn";
    public const string HitSlash = $"{SceneRoot}/gloomy_monster_hit/hit_slash_normal_01.tscn";
    public const string HitBlunt = $"{SceneRoot}/gloomy_monster_hit/hit_blunt_normal_01.tscn";

    public const string PrimeAttackPlay1Sfx = $"{SoundRoot}/se_gloomy_prime_normal_attack_play1.wav";
    public const string PrimeAttackPlay2Sfx = $"{SoundRoot}/se_gloomy_prime_normal_attack_play2.wav";
    public const string PrimeBuffSfx = $"{SoundRoot}/se_gloomy_prime_normal_buff_1.wav";
    public const string PrimeUniqueSfx = $"{SoundRoot}/se_gloomy_prime_normal_unique_1.wav";
    public const string BeastBuffSfx = $"{SoundRoot}/se_gloomy_beast_normal_buff_1.wav";
    public const string BeastAttack01Sfx = $"{SoundRoot}/se_gloomy_beast_normal_m_attack_01.wav";
    public const string BeastAttack02Sfx = $"{SoundRoot}/se_gloomy_beast_normal_m_attack_02.wav";

    public static bool PlaySfx(string soundPath, float volume = 1f) =>
        MeiLinAudioService.TryPlayResource(soundPath, volume);

    public static void PlaySelf(Creature caster, string scenePath, float scale = 1f)
    {
        MeiLinVfxHelper.PlayAtCreature(scenePath, caster, uniformScale: scale, followCreature: true);
    }

    public static void PlayTarget(Creature? target, string scenePath, float scale = 1f, float rotationDegrees = 0f)
    {
        var instance = MeiLinVfxHelper.PlayAtCreature(scenePath, target, uniformScale: scale);
        if (instance != null)
            instance.RotationDegrees = rotationDegrees;
    }

    public static async Task ShakeAfter(float delaySeconds)
    {
        if (delaySeconds > 0f)
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

        var room = NCombatRoom.Instance;
        if (room != null && GodotObject.IsInstanceValid(room))
            await MeiLinTimelineCameraShake.PlayAsync(room);
    }
}
