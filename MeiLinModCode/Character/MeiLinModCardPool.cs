using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Config;

namespace MeiLinMod.MeiLinModCode.Character;

public class MeiLinModCardPool : CustomCardPoolModel
{
    private const string ChaosCardFramePath = "res://MeiLinMod/images/cards/chaos_frame/card_frame_chaos_s.tres";

    public override string Title => MeiLinMod.CharacterId; //This is not a display name.
    //public override string EnergyColorName => MeiLinMod.CharacterId;
    public override string EnergyColorName => "ironclad";
    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,DeckEntryCardColor
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 1f; //Hue; changes the color.
    public override float S => 1f; //Saturation
    public override float V => 1f; //Brightness

    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load MeiLinMod/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("FFC0CB");
    //public override Color ShaderColor => new(0.5f, 0.5f, 1f);

    public override Texture2D? CustomFrame(CustomCardModel card)
    {
        if (!MeiLinModConfig.UseChaosCardDynamicPortraits)
            return null;

        if (card is not MeiLinModCard { UseCustomAncientFrame: true })
            return null;

        return PreloadManager.Cache.GetAsset<Texture2D>(ChaosCardFramePath);
    }

    public override bool IsColorless => false;
}
