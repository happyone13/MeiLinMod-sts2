using System;
using System.Collections.Generic;
using Godot;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MegaCrit.Sts2.Core.Entities.Players;

namespace MeiLinMod.MeiLinModCode.Services;

public static class MeiLinAudioService
{
    private const float MeiLinVoiceGain = 2f;
    private const float UltimateVoiceScale = 0.28f;
    private const float UltimateEffectScale = 0.16f;

    private static readonly string[] FmodPrefixes = ["event:/", "snapshot:/", "bus:/", "vca:/", "parameter:/"];

    private static readonly string[] AttackPool =
    [
        "res://MeiLinMod/sound/meilin_attack1.mp3",
        "res://MeiLinMod/sound/meilin_attack2.mp3",
        "res://MeiLinMod/sound/meilin_attack3.mp3"
    ];

    private static readonly string[] CastPool =
    [
        "res://MeiLinMod/sound/meilin_cast1.mp3",
        "res://MeiLinMod/sound/meilin_cast2.mp3",
        "res://MeiLinMod/sound/meilin_cast3.mp3",
        "res://MeiLinMod/sound/meilin_cast4.mp3",
        "res://MeiLinMod/sound/meilin_cast5.mp3"
    ];
    private static readonly string[] GongPool =
    [
        "res://MeiLinMod/sound/meilin_gong1.mp3",
        "res://MeiLinMod/sound/meilin_gong2.mp3"
    ];
    private static readonly string[] YuPool =
    [
        "res://MeiLinMod/sound/meilin_yu1.mp3",
        "res://MeiLinMod/sound/meilin_yu2.mp3"
    ];

    private const string DiePath = "res://MeiLinMod/sound/meilin_die.mp3";
    private const string SelectPath = "res://MeiLinMod/sound/meilin_select.mp3";
    private const string AttackDefenseUnityPath = "res://MeiLinMod/sound/attack_defense_unity.mp3";
    private const string FireDragonGemPath = "res://MeiLinMod/sound/fire_dragon_gam.mp3";
    private const string HuoLongJingTianPath = "res://MeiLinMod/sound/huo_long_jing_tian.mp3";
    private const string ShengLongJiaoPath = "res://MeiLinMod/sound/sheng_long_jiao.mp3";
    private const string ZuiZhongAoYiYanLongJiangLinPath = "res://MeiLinMod/sound/zui_zhong_ao_yi_yan_long_jiang_lin.mp3";
    private const string UgVoicePath = "res://MeiLinMod/sound/vo_1027_ug.wav";
    private const string UgSoundPath = "res://MeiLinMod/sound/se_1027_ug_attack.wav";
    private const string UxVoicePath = "res://MeiLinMod/sound/vo_1027_ux.wav";
    private const string UxSoundPath = "res://MeiLinMod/sound/se_1027_ux_buff.wav";

    private static readonly Dictionary<string, string> CustomCardClipMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["attack_defense_unity"] = AttackDefenseUnityPath,
        ["fire_dragon_gam"] = FireDragonGemPath,
        ["huo_long_jing_tian"] = HuoLongJingTianPath,
        ["sheng_long_jiao"] = ShengLongJiaoPath,
        ["zui_zhong_ao_yi_yan_long_jiang_lin"] = ZuiZhongAoYiYanLongJiangLinPath
    };

    private static Node? _audioHost;
    private static long _playerCounter;
    private static int _suppressNextAttackSfxCount;
    private static int _suppressNextCastSfxCount;

    public static bool TryPlayFromSfxCmd(string sfx, float linearVolume)
    {
        if (string.IsNullOrWhiteSpace(sfx))
            return false;

        var key = sfx.Trim();
        var lower = key.ToLowerInvariant();

        foreach (var prefix in FmodPrefixes)
        {
            if (lower.StartsWith(prefix, StringComparison.Ordinal) && !lower.Contains("meilin"))
                return false;
        }

        if (!TryResolvePath(lower, out var path))
            return false;

        return TryPlay(path, linearVolume);
    }

    public static bool TryPlayDeath(Player? player, float linearVolume = 1f)
    {
        if (!IsMeiLinPlayer(player))
            return false;

        return TryPlay(DiePath, linearVolume);
    }

    public static bool TryPlayAttackStanceSwitch(Player? player = null, float linearVolume = 1f)
    {
        if (!IsMeiLinPlayer(player))
            return false;

        return TryPlay(PickRandom(GongPool), linearVolume);
    }

    public static bool TryPlayGuardStanceSwitch(Player? player = null, float linearVolume = 1f)
    {
        if (!IsMeiLinPlayer(player))
            return false;

        return TryPlay(PickRandom(YuPool), linearVolume);
    }

    public static bool TryPlayAttackVoice(Player? player = null, float linearVolume = 1f)
    {
        if (!IsMeiLinPlayer(player))
            return false;

        return TryPlay(PickRandom(AttackPool), linearVolume);
    }

    public static bool TryPlayCustomCardClip(string clipKey, Player? player = null, float linearVolume = 1f)
    {
        if (!IsMeiLinPlayer(player))
            return false;

        if (!CustomCardClipMap.TryGetValue(clipKey, out var path))
            return false;

        return TryPlay(path, linearVolume);
    }

    public static bool TryPlayUgAttackVoice(Player? player = null, float linearVolume = 1f)
    {
        return IsMeiLinPlayer(player) && TryPlay(UgVoicePath, linearVolume * UltimateVoiceScale);
    }

    public static bool TryPlayUgAttackSound(Player? player = null, float linearVolume = 1f)
    {
        return IsMeiLinPlayer(player) && TryPlay(UgSoundPath, linearVolume * UltimateEffectScale);
    }

    public static bool TryPlayUxVoice(Player? player = null, float linearVolume = 1f)
    {
        return IsMeiLinPlayer(player) && TryPlay(UxVoicePath, linearVolume * UltimateVoiceScale);
    }

    public static bool TryPlayUxSound(Player? player = null, float linearVolume = 1f)
    {
        return IsMeiLinPlayer(player) && TryPlay(UxSoundPath, linearVolume * UltimateEffectScale);
    }

    public static void SuppressNextDefaultAttackSfx(Player? player = null)
    {
        if (!IsMeiLinPlayer(player))
            return;

        _suppressNextAttackSfxCount++;
    }

    public static void SuppressNextDefaultCastSfx(Player? player = null)
    {
        if (!IsMeiLinPlayer(player))
            return;

        _suppressNextCastSfxCount++;
    }

    public static bool ShouldSuppressDefaultSfx(string sfx)
    {
        if (string.IsNullOrWhiteSpace(sfx))
            return false;

        var key = sfx.Trim().ToLowerInvariant();
        if (key == "meilin_attack" && _suppressNextAttackSfxCount > 0)
        {
            _suppressNextAttackSfxCount--;
            return true;
        }

        if (key == "meilin_cast" && _suppressNextCastSfxCount > 0)
        {
            _suppressNextCastSfxCount--;
            return true;
        }

        return false;
    }

    private static bool TryResolvePath(string key, out string path)
    {
        if (!key.Contains("meilin", StringComparison.Ordinal))
        {
            path = string.Empty;
            return false;
        }

        if (key.Contains("attack", StringComparison.Ordinal))
        {
            path = PickRandom(AttackPool);
            return true;
        }

        if (key.Contains("cast", StringComparison.Ordinal))
        {
            path = PickRandom(CastPool);
            return true;
        }

        if (key.Contains("die", StringComparison.Ordinal) || key.Contains("death", StringComparison.Ordinal))
        {
            path = DiePath;
            return true;
        }

        if (key.Contains("select", StringComparison.Ordinal) || key.Contains("pick", StringComparison.Ordinal))
        {
            path = SelectPath;
            return true;
        }

        path = string.Empty;
        return false;
    }

    private static bool TryPlay(string resourcePath, float linearVolume)
    {
        var stream = ResourceLoader.Load<AudioStream>(resourcePath, cacheMode: ResourceLoader.CacheMode.Reuse);
        if (stream == null)
        {
            MainFile.Logger.Warn($"[Audio] Failed to load stream: {resourcePath}");
            return false;
        }

        var host = EnsureHostNode();
        if (host == null)
            return false;

        var player = new AudioStreamPlayer
        {
            Name = $"MeiLinSfx_{++_playerCounter}",
            Stream = stream,
            VolumeDb = LinearToDb(linearVolume * MeiLinSharedSettings.VoiceVolume * MeiLinVoiceGain)
        };

        host.AddChild(player);
        player.Finished += () => player.QueueFree();
        player.Play();
        return true;
    }

    public static bool TryPlayResource(string resourcePath, float linearVolume = 1f)
    {
        return TryPlay(resourcePath, linearVolume);
    }

    private static Node? EnsureHostNode()
    {
        if (_audioHost != null && GodotObject.IsInstanceValid(_audioHost) && _audioHost.IsInsideTree())
            return _audioHost;

        if (Engine.GetMainLoop() is not SceneTree tree || tree.Root == null)
            return null;

        _audioHost = tree.Root.GetNodeOrNull<Node>("MeiLinAudioHost");
        if (_audioHost != null)
            return _audioHost;

        _audioHost = new Node
        {
            Name = "MeiLinAudioHost",
            ProcessMode = Node.ProcessModeEnum.Always
        };
        tree.Root.AddChild(_audioHost);
        return _audioHost;
    }

    private static string PickRandom(string[] pool)
    {
        var index = (int)GD.RandRange(0, pool.Length - 1);
        return pool[index];
    }

    private static float LinearToDb(float linearVolume)
    {
        if (linearVolume <= 0f)
            return -80f;

        return Mathf.LinearToDb(Mathf.Max(linearVolume, 0.0001f));
    }

    private static bool IsMeiLinPlayer(Player? player)
    {
        if (player?.Character == null)
            return false;

        var id = player.Character.Id.Entry ?? string.Empty;
        return id.Contains("MEILIN", StringComparison.OrdinalIgnoreCase);
    }
}
