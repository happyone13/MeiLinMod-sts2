using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Patches;
using TestTheSpire;
using Xunit;
using Character = MeiLinMod.MeiLinModCode.Character.MeiLinMod;
using Frame = MeiLinMod.MeiLinModCode.Patches.CardCustomAncientFramePatch;
using Portrait = MeiLinMod.MeiLinModCode.Patches.CardSpinePortraitPatch;

namespace MeiLinMod.Tests;

public sealed class CardVisualOwnershipTests : CombatTestSuite
{
    private const string Prefix = "MeiLinChaos";

    protected override void ConfigureBattle(CombatTestBattleBuilder battle) =>
        battle.Player<Character>().AddEnemy<BigDummy>().WithSeed("meilin-card-ownership");

    [Fact]
    public async Task Real_card_refresh_reuses_nodes_and_hands_foreign_UI_back_before_redraw()
    {
        await Play(await AddToHand<DefendMeilin>());
        var owned = await AddToHand<DefendMeilin>();
        var vanilla = await AddToHand<StrikeIronclad>();
        var card = CreateCard();
        var reference = CreateCard();
        try
        {
            reference.Model = vanilla;
            card.Model = vanilla;
            Refresh(reference);
            Refresh(card);
            AssertForeignUntouched(card, portraitFirst: true);
            card.Model = owned;
            Refresh(card);
            var cost = card.Body.GetNode<Control>(Prefix + "CostLine");
            var type = card.Body.GetNode<Label>(Prefix + "CategoryText");
            var upgrade = card.Body.GetNode<Control>(Prefix + "UpgradeIcon");
            ulong costId = cost.GetInstanceId(), typeId = type.GetInstanceId();
            Assert.False(upgrade.Visible);
            owned.UpgradeInternal();
            owned.EnergyCost.SetThisTurn(0);
            Refresh(card);
            Assert.True(upgrade.Visible);
            Assert.Equal("0", card.GetNode<Label>("%EnergyLabel").Text);
            var energy = card.GetNode<Label>("%EnergyLabel");
            energy.AddThemeColorOverride("font_color", Colors.Lime);
            Frame.Apply(card);
            var digit = card.Body.GetNode<Control>(Prefix + "CostText/CostDigit0");
            ulong digitId = digit.GetInstanceId();
            Frame.Apply(card);
            Assert.Equal(digitId, card.Body.GetNode<Control>(Prefix + "CostText/CostDigit0").GetInstanceId());
            Assert.Equal(costId, card.Body.GetNode<Control>(Prefix + "CostLine").GetInstanceId());
            Assert.Equal(typeId, card.Body.GetNode<Label>(Prefix + "CategoryText").GetInstanceId());

            card.Model = vanilla;
            Refresh(card);
            Assert.False(GodotObject.IsInstanceValid(cost));
            Assert.Equal(reference.GetNode<TextureRect>("%Portrait").Texture, card.GetNode<TextureRect>("%Portrait").Texture);
            foreach (string path in new[] { "%TitleLabel", "%EnergyIcon", "%TypeLabel", "%Frame" })
            {
                var expected = reference.GetNode<Control>(path);
                var actual = card.GetNode<Control>(path);
                Assert.Equal(expected.Position, actual.Position);
                Assert.Equal(expected.Visible, actual.Visible);
                Assert.Equal(expected.GetIndex(), actual.GetIndex());
            }
            AssertForeignUntouched(card, portraitFirst: false);
        }
        finally
        {
            card.Free();
            reference.Free();
        }
    }

    [Fact]
    public async Task Dynamic_portrait_reuses_only_its_own_model_and_native_pool_clear_wins()
    {
        await Play(await AddToHand<DefendMeilin>());
        var first = await AddToHand<FireDragonGem>();
        var second = await AddToHand<FireDragonGem>();
        var vanilla = await AddToHand<StrikeIronclad>();
        var card = CreateCard();
        bool previous = MeiLinSharedSettings.DynamicCardPortraitsEnabled;
        try
        {
            MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(true, persist: false);
            var ancient = card.GetNode<TextureRect>("%AncientPortrait");
            var foreign = new Control { Name = "OtherSkinOverlay" };
            ancient.AddChild(foreign);
            card.Model = first;
            Refresh(card);
            var overlay = ancient.GetNode<Control>(Portrait.SpineOverlayNodeName);
            ulong oldId = overlay.GetInstanceId();
            Refresh(card);
            Assert.Equal(oldId, ancient.GetNode<Control>(Portrait.SpineOverlayNodeName).GetInstanceId());
            card.Model = second;
            Refresh(card);
            Assert.False(GodotObject.IsInstanceValid(overlay));
            Assert.NotEqual(oldId, ancient.GetNode<Control>(Portrait.SpineOverlayNodeName).GetInstanceId());
            card.OnFreedToPool();
            Assert.Null(card.GetNode<TextureRect>("%Portrait").Texture);
            Assert.Null(ancient.GetNodeOrNull<Control>(Portrait.SpineOverlayNodeName));
            Assert.Same(ancient, foreign.GetParent());
            card.Model = vanilla;
            Refresh(card);
            AssertForeignUntouched(card, portraitFirst: true);
        }
        finally
        {
            card.Free();
            MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(previous, persist: false);
        }
    }

    private static void AssertForeignUntouched(NCard card, bool portraitFirst)
    {
        var normal = card.GetNode<TextureRect>("%Portrait");
        var ancient = card.GetNode<TextureRect>("%AncientPortrait");
        var foreign = new Control { Name = Prefix + "CostLine", Visible = false };
        var oldTexture = normal.Texture;
        bool oldVisibility = normal.Visible, oldClip = ancient.ClipContents;
        card.Body.AddChild(foreign);
        try
        {
            normal.Texture = null;
            normal.Visible = false;
            ancient.ClipContents = false;
            if (portraitFirst) Portrait.RemoveSpineOverlay(card);
            Frame.PrepareForBaseVisuals(card);
            Frame.Apply(card);
            Frame.CleanupPooledCard(card);
            Portrait.Apply(card);
            if (!portraitFirst) Portrait.RemoveSpineOverlay(card);
            Assert.Null(normal.Texture);
            Assert.False(normal.Visible);
            Assert.False(ancient.ClipContents);
            Assert.Same(card.Body, foreign.GetParent());
            Assert.False(foreign.Visible);
        }
        finally
        {
            if (GodotObject.IsInstanceValid(foreign)) foreign.Free();
            normal.Texture = oldTexture;
            normal.Visible = oldVisibility;
            ancient.ClipContents = oldClip;
        }
    }

    private static NCard CreateCard(Node? parent = null)
    {
        var result = ResourceLoader.Load<PackedScene>("res://scenes/cards/card.tscn").Instantiate<NCard>();
        result.Scale = Vector2.One * 1.2f;
        (parent ?? ((SceneTree)Engine.GetMainLoop()).Root).AddChild(result);
        return result;
    }

    private static void Refresh(NCard card) => card.UpdateVisuals(PileType.Hand, CardPreviewMode.Normal);

    [YukiLoadedFact]
    public async Task CoLoaded_MeiLin_to_Yuki_to_MeiLin_to_vanilla_relinquishes_the_real_card()
    {
        await Play(await AddToHand<DefendMeilin>());
        var meilin = await AddToHand<DefendMeilin>();
        var vanilla = await AddToHand<StrikeIronclad>();
        var yukiType = YukiAssembly()!.GetType("YukiMod.YukiModCode.Cards.DefendYuki", throwOnError: true)!;
        var yuki = Combat.CreateCard(ModelDb.GetById<CardModel>(ModelDb.GetId(yukiType)), Player);
        await CardPileCmd.AddGeneratedCardToCombat(yuki, PileType.Hand, Player, CardPilePosition.Top);
        SubViewport? viewport = null;
        if (DisplayServer.GetName() != "headless")
        {
            viewport = new SubViewport
            {
                Size = new Vector2I(1280, 720), TransparentBg = true, Disable3D = true,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always
            };
            ((SceneTree)Engine.GetMainLoop()).Root.AddChild(viewport);
        }
        var card = CreateCard(viewport);
        var reference = CreateCard(viewport);
        try
        {
            reference.Model = vanilla;
            Refresh(reference);
            var marker = new Control { Name = "UnrelatedSkinMarker", Modulate = Colors.Teal, Visible = false };
            card.Body.AddChild(marker);
            const string meiCost = "MeiLinChaosCostLine", yukiCost = "YukiChaosCostLine";

            Control TakeOver(CardModel model, string active, string departed)
            {
                card.Model = model;
                Refresh(card);
                var cost = card.Body.GetNode<Control>(active);
                Assert.NotNull(cost);
                Assert.Null(card.Body.GetNodeOrNull<Control>(departed));
                Assert.Equal(model.Portrait, card.GetNode<TextureRect>("%AncientPortrait").Texture);
                Refresh(card);
                Assert.Same(cost, card.Body.GetNode<Control>(active));
                Assert.Equal(Colors.Teal, marker.Modulate);
                Assert.False(marker.Visible);
                return cost;
            }

            var meiFirst = TakeOver(meilin, meiCost, yukiCost);
            var yukiSecond = TakeOver(yuki, yukiCost, meiCost);
            Assert.False(GodotObject.IsInstanceValid(meiFirst));
            var meiThird = TakeOver(meilin, meiCost, yukiCost);
            Assert.False(GodotObject.IsInstanceValid(yukiSecond));
            card.Model = vanilla;
            Refresh(card);
            Assert.False(GodotObject.IsInstanceValid(meiThird));
            Assert.Null(card.Body.GetNodeOrNull<Control>(meiCost));
            Assert.Null(card.Body.GetNodeOrNull<Control>(yukiCost));
            foreach (string path in new[] { "%Portrait", "%AncientPortrait", "%EnergyIcon", "%Frame" })
            {
                var actual = card.GetNode<TextureRect>(path);
                var expected = reference.GetNode<TextureRect>(path);
                Assert.Equal(expected.Texture, actual.Texture);
                Assert.Equal(expected.Visible, actual.Visible);
                Assert.Equal(expected.Position, actual.Position);
            }
            Assert.Same(card.Body, marker.GetParent());
            Assert.Equal(Colors.Teal, marker.Modulate);
            Assert.False(marker.Visible);
            AssertForeignUntouched(card, portraitFirst: true);
            if (viewport != null)
                await CompareVanillaRendering(viewport, card, reference);
        }
        finally
        {
            card.Free();
            reference.Free();
            viewport?.Free();
        }
    }

    private static async Task CompareVanillaRendering(SubViewport viewport, NCard reused, NCard reference,
        [System.Runtime.CompilerServices.CallerFilePath] string sourcePath = "")
    {
        int tile = viewport.Size.X / 2;
        var center = new Vector2(tile / 2, viewport.Size.Y / 2);
        reused.GlobalPosition += center - reused.GetNode<Control>("%Frame").GetGlobalRect().GetCenter();
        reference.GlobalPosition += center + Vector2.Right * tile - reference.GetNode<Control>("%Frame").GetGlobalRect().GetCenter();
        var tree = (SceneTree)Engine.GetMainLoop();
        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        await tree.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        using var image = viewport.GetTexture().GetImage();
        var output = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", "..", "tmp", "card-frame-coload-vanilla.png"));
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        Assert.Equal(Error.Ok, image.SavePng(output));
        int drawn = 0, different = 0;
        var colors = new HashSet<Color>();
        for (int y = 0; y < image.GetHeight(); y += 4)
        for (int x = 0; x < tile; x += 4)
        {
            var a = image.GetPixel(x, y);
            var b = image.GetPixel(x + tile, y);
            if (Math.Max(a.A, b.A) < 0.08f) continue;
            drawn++;
            colors.Add(b);
            if (Math.Abs(a.R - b.R) > 0.05f || Math.Abs(a.G - b.G) > 0.05f ||
                Math.Abs(a.B - b.B) > 0.05f || Math.Abs(a.A - b.A) > 0.05f)
                different++;
        }
        Assert.True(drawn > 1000 && colors.Count > 32, "The reference card must render as a nonblank image.");
        Assert.True(different <= drawn * 0.01, $"Reused (left) and fresh (right) Strike differ at {different}/{drawn} drawn sample points: {output}");
    }

    private static System.Reflection.Assembly? YukiAssembly() =>
        AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == "YukiMod");

    private sealed class YukiLoadedFactAttribute : FactAttribute
    {
        public YukiLoadedFactAttribute()
        {
            if (YukiAssembly() == null)
                Skip = "Requires MeiLinMod and YukiMod loaded in the same game process.";
        }
    }
}
