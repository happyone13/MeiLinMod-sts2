using System.Text.RegularExpressions;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using Theresa.TheresaCode.Character;

namespace Theresa.TheresaCode.Cards;

[Pool(typeof(TheresaCardPool))]
public abstract partial class TheresaCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target, bool showInCardLibrary = true, bool autoAdd = true) : CustomCardModel(baseCost, type, rarity, target, showInCardLibrary, autoAdd)
{

    /// <summary>
    /// 自定义 Spine 动画肖像场景路径。如果子类 override 返回一个 .tscn 路径，
    /// SovereignSpinePortraitPatch 会用它来替换 AncientPortrait 的纹理。
    /// </summary>
    public virtual string? CustomSpinePortraitScenePath => null;
}
