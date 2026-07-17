using System.Collections.Generic;
using MeiLinMod.MeiLinModCode.Monsters.Gloomy;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;

namespace MeiLinMod.MeiLinModCode.Encounters;

/// <summary>
///     阴郁兽群遭遇：战斗开场生成 1 只阴郁首领（1006012）与 3 只阴郁野兽（1006017）。
///     仅在第三幕（Glory）的普通怪物池中出现。
/// </summary>
[RegisterActEncounter(typeof(Glory))]
public sealed class GloomyPackEncounter : ModEncounterTemplate
{
    private const string PlayerEscapedStateKey = "player_escaped";
    private const string Beast1Slot = "1006017_1";
    private const string Beast2Slot = "1006017_2";
    private const string PrimeSlot = "1006012";
    private const string Beast3Slot = "1006017_3";

    public bool WasPlayerEscape { get; private set; }

    public bool EscapeCardsDealt { get; private set; }

    public override RoomType RoomType => RoomType.Monster;

    public override bool ShouldGiveRewards => !WasPlayerEscape;

    public override bool IsValidForAct(ActModel act) =>
        GloomyEncounterSharedSettings.Enabled && GloomyEncounterSharedSettings.IsActiveProvider(MainFile.ModId);

    public override IReadOnlyList<string> Slots =>
    [
        Beast1Slot,
        Beast2Slot,
        PrimeSlot,
        Beast3Slot
    ];

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://MeiLinMod/scenes/monsters/gloomy/encounters/monster_encounter.tscn"
    );

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GloomyPrimeMonster>(),
        ModelDb.Monster<GloomyBeastMonster>()
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<GloomyPrimeMonster>().ToMutable(), PrimeSlot),
            (ModelDb.Monster<GloomyBeastMonster>().ToMutable(), Beast1Slot),
            (ModelDb.Monster<GloomyBeastMonster>().ToMutable(), Beast2Slot),
            (ModelDb.Monster<GloomyBeastMonster>().ToMutable(), Beast3Slot)
        ];
    }

    public void MarkPlayerEscaped()
    {
        AssertMutable();
        WasPlayerEscape = true;
    }

    public void MarkEscapeCardsDealt()
    {
        AssertMutable();
        EscapeCardsDealt = true;
    }

    public override float CalculateGoldProportion(MegaCrit.Sts2.Core.Combat.CombatState combatState) =>
        WasPlayerEscape ? 0f : base.CalculateGoldProportion(combatState);

    public override Dictionary<string, string> SaveCustomState()
    {
        var state = base.SaveCustomState();
        state[PlayerEscapedStateKey] = WasPlayerEscape.ToString();
        return state;
    }

    public override void LoadCustomState(Dictionary<string, string> state)
    {
        base.LoadCustomState(state);
        WasPlayerEscape = state.TryGetValue(PlayerEscapedStateKey, out var value)
                          && bool.TryParse(value, out var escaped)
                          && escaped;
    }
}
