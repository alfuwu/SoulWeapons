using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Content;
using ReLogic.Graphics;
using SoulWeapons.Content.Items;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SoulWeapons;

public class SoulWeapons : Mod {
    public const string Localization = $"Mods.{nameof(SoulWeapons)}";
    private static Texture2D katakana;
    private static (char katakana, DynamicSpriteFont.SpriteCharacterData texture)[] katakanaCharacters;

    public Hook h;

    public override void Load() {
        if (!Main.dedServ) {
            GameShaders.Misc[$"{nameof(SoulWeapons)}/EnergySword"] = new(Assets.Request<Effect>("Content/EnergySword"), "ScreenPass");
            GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"] = new(Assets.Request<Effect>("Content/Weapon"), "ScreenPass");
        }

        katakana = Assets.Request<Texture2D>("katakana", AssetRequestMode.ImmediateLoad).Value;
        katakanaCharacters = [
            ('ア', new(katakana, new Rectangle(0, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('イ', new(katakana, new Rectangle(13, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ウ', new(katakana, new Rectangle(26, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('エ', new(katakana, new Rectangle(39, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 12, 1))),
            ('オ', new(katakana, new Rectangle(52, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('カ', new(katakana, new Rectangle(65, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('キ', new(katakana, new Rectangle(78, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ク', new(katakana, new Rectangle(91, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ケ', new(katakana, new Rectangle(104, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('コ', new(katakana, new Rectangle(117, 0, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ガ', new(katakana, new Rectangle(133, 0, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ギ', new(katakana, new Rectangle(149, 0, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('グ', new(katakana, new Rectangle(165, 0, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ゲ', new(katakana, new Rectangle(181, 0, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ゴ', new(katakana, new Rectangle(197, 0, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('サ', new(katakana, new Rectangle(65, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('シ', new(katakana, new Rectangle(78, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ス', new(katakana, new Rectangle(91, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('セ', new(katakana, new Rectangle(104, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ソ', new(katakana, new Rectangle(117, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ザ', new(katakana, new Rectangle(133, 15, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ジ', new(katakana, new Rectangle(149, 15, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ズ', new(katakana, new Rectangle(165, 15, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ゼ', new(katakana, new Rectangle(181, 15, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('ゾ', new(katakana, new Rectangle(197, 15, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 15, 1))),
            ('タ', new(katakana, new Rectangle(65, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('テ', new(katakana, new Rectangle(78, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ト', new(katakana, new Rectangle(91, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ツ', new(katakana, new Rectangle(104, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('チ', new(katakana, new Rectangle(117, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ナ', new(katakana, new Rectangle(65, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ニ', new(katakana, new Rectangle(78, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヌ', new(katakana, new Rectangle(91, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ネ', new(katakana, new Rectangle(104, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ノ', new(katakana, new Rectangle(117, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('マ', new(katakana, new Rectangle(65, 60, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ミ', new(katakana, new Rectangle(78, 60, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ム', new(katakana, new Rectangle(91, 60, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('メ', new(katakana, new Rectangle(104, 60, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('モ', new(katakana, new Rectangle(117, 60, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ラ', new(katakana, new Rectangle(65, 75, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('リ', new(katakana, new Rectangle(78, 75, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ル', new(katakana, new Rectangle(91, 75, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('レ', new(katakana, new Rectangle(104, 75, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ロ', new(katakana, new Rectangle(117, 75, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ハ', new(katakana, new Rectangle(65, 90, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヒ', new(katakana, new Rectangle(78, 90, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('フ', new(katakana, new Rectangle(91, 90, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヘ', new(katakana, new Rectangle(104, 90, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ホ', new(katakana, new Rectangle(213, 90, 13, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 13, 1))),
            ('バ', new(katakana, new Rectangle(133, 90, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ビ', new(katakana, new Rectangle(149, 90, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ブ', new(katakana, new Rectangle(165, 90, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ベ', new(katakana, new Rectangle(181, 90, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ボ', new(katakana, new Rectangle(197, 90, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('パ', new(katakana, new Rectangle(133, 115, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ピ', new(katakana, new Rectangle(149, 115, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('プ', new(katakana, new Rectangle(165, 115, 12, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ペ', new(katakana, new Rectangle(181, 115, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ポ', new(katakana, new Rectangle(197, 115, 15, 14), new Rectangle(0, 0, 16, 14), new Vector3(1, 16, 1))),
            ('ワ', new(katakana, new Rectangle(0, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヲ', new(katakana, new Rectangle(13, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ン', new(katakana, new Rectangle(26, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ー', new(katakana, new Rectangle(52, 15, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ャ', new(katakana, new Rectangle(0, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ュ', new(katakana, new Rectangle(13, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ョ', new(katakana, new Rectangle(26, 30, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヤ', new(katakana, new Rectangle(0, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ユ', new(katakana, new Rectangle(13, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1))),
            ('ヨ', new(katakana, new Rectangle(26, 45, 12, 14), new Rectangle(0, 0, 14, 14), new Vector3(1, 13, 1)))
        ];

        IL_ItemSlot.DrawItemIcon += DrawItemIcon;
        IL_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawPlayer_27_HeldItem;
        IL_Player.ItemCheck_ApplyUseStyle_Inner += ItemCheck_ApplyUseStyle_Inner;

        h = new(typeof(DynamicSpriteFont).GetMethod("GetCharacterData"), (Func<DynamicSpriteFont, char, DynamicSpriteFont.SpriteCharacterData> orig, DynamicSpriteFont font, char c) => {
            if (font == FontAssets.MouseText.Value)
                foreach ((char katakana, DynamicSpriteFont.SpriteCharacterData texture) in katakanaCharacters)
                    if (c == katakana)
                        return texture;
            return orig(font, c);
        });
        h.Apply();
    }

    public override void Unload() {
        IL_ItemSlot.DrawItemIcon -= DrawItemIcon;
        IL_PlayerDrawLayers.DrawPlayer_27_HeldItem -= DrawPlayer_27_HeldItem;
        IL_Player.ItemCheck_ApplyUseStyle_Inner -= ItemCheck_ApplyUseStyle_Inner;

        h?.Undo();
    }

    private void DrawItemIcon(ILContext il) {
        try {
            ILCursor c = new(il);
            c.GotoNext(i => i.MatchLdsfld("Terraria.GameContent.TextureAssets", "Item"));
            ILLabel vanilla = il.DefineLabel();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon);
            c.Emit(OpCodes.Brfalse_S, vanilla);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Item item) => {
                SoulWeapon s = item.ModItem as SoulWeapon;
                if (s.texture == null)
                    s.ConstructTexture();
                return s.texture;
            });
            c.Emit(OpCodes.Stloc_1);
            ILLabel skipVanilla = il.DefineLabel();
            c.Emit(OpCodes.Br_S, skipVanilla);
            c.MarkLabel(vanilla);
            c.GotoNext(MoveType.After, i => i.MatchStloc1());
            c.MarkLabel(skipVanilla);
        } catch (Exception e) {
            MonoModHooks.DumpIL(this, il);
            throw new ILPatchFailureException(this, il, e);
        }
    }

    private void DrawPlayer_27_HeldItem(ILContext il) {
        try {
            ILCursor c = new(il);
            // replacing texture
            c.GotoNext(i => i.MatchLdsfld("Terraria.GameContent.TextureAssets", "Item"));
            ILLabel vanilla = il.DefineLabel();
            c.Emit(OpCodes.Ldloc_0); // load item var
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon);
            c.Emit(OpCodes.Brfalse_S, vanilla);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Item item) => (item.ModItem as SoulWeapon).texture);
            c.Emit(OpCodes.Stloc_3);
            ILLabel skipVanilla = il.DefineLabel();
            c.Emit(OpCodes.Br_S, skipVanilla);
            c.MarkLabel(vanilla);
            c.GotoNext(MoveType.After, i => i.MatchStloc3());
            c.MarkLabel(skipVanilla);

            // replacing item rectangle
            c.GotoNext(i => i.MatchLdarg0(),
                i => i.MatchLdfld<PlayerDrawSet>("drawPlayer"));
            ILLabel vanilla2 = il.DefineLabel();
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon);
            c.Emit(OpCodes.Brfalse_S, vanilla2);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Item item) => new Rectangle(0, 0, item.width, item.height));
            c.Emit(OpCodes.Stloc_S, (byte)5);
            ILLabel skipVanilla2 = il.DefineLabel();
            c.Emit(OpCodes.Br_S, skipVanilla2);
            c.MarkLabel(vanilla2);
            c.GotoNext(MoveType.After, i => i.MatchStloc(5));
            c.MarkLabel(skipVanilla2);

            // soul staves
            ILLabel skipVanilla3 = il.DefineLabel();
            c.GotoNext(i => i.MatchLdsfld<Item>("staff"));
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon s && (s.type == WeaponType.Scepter || s.type == WeaponType.Staff));
            c.Emit(OpCodes.Brtrue_S, skipVanilla3);
            c.GotoNext(MoveType.After, i => i.MatchBrfalse(out _));
            c.MarkLabel(skipVanilla3);
        } catch (Exception e) {
            MonoModHooks.DumpIL(this, il);
            throw new ILPatchFailureException(this, il, e);
        }
    }

    private void ItemCheck_ApplyUseStyle_Inner(ILContext il) {
        try {
            ILCursor c = new(il);
            ILLabel skipVanilla = il.DefineLabel();
            c.GotoNext(i => i.MatchLdsfld<Item>("staff"));
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon s && (s.type == WeaponType.Scepter || s.type == WeaponType.Staff));
            c.Emit(OpCodes.Brtrue_S, skipVanilla);
            c.GotoNext(MoveType.After, i => i.MatchBrfalse(out _));
            c.MarkLabel(skipVanilla);
        } catch (Exception e) {
            MonoModHooks.DumpIL(this, il);
            throw new ILPatchFailureException(this, il, e);
        }
    }
}

public class SoulWeaponsConfig : ModConfig {
    public static SoulWeaponsConfig Instance { get; private set; }

    public override ConfigScope Mode => ConfigScope.ServerSide;

    [DefaultValue(false)]
    public bool SoulWeaponsTalk { get; set; }

    [DefaultValue(0.2f)]
    public float SoulWeaponPreHardmodeStage { get; set; }

    [DefaultValue(0.5f)]
    public float SoulWeaponHardmodeStage { get; set; }

    public override void OnLoaded() => Instance = this;
}

public class UUID(byte[] bytes) {
    readonly Guid guid = new(bytes);

    public UUID() : this(new byte[16].Select(b => (byte)Main.rand.Next(256)).ToArray()) { }

    public byte[] ToByteArray() => guid.ToByteArray();
    public override string ToString() => guid.ToString();

    public override bool Equals(object obj) => obj is UUID u && u == this || obj is Guid g && g == guid;
    public override int GetHashCode() => guid.GetHashCode();
    public static bool operator ==(UUID left, UUID right) => left.guid == right.guid;
    public static bool operator !=(UUID left, UUID right) => left.guid != right.guid;
}

public class SoulWieldingPlayer : ModPlayer {
    public UUID SoulWeaponID { get; set; }

    public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item) => item.ModItem is not SoulWeapon s || !s.CanPlayerWield(Player);

    public override void SaveData(TagCompound tag) {
        if (SoulWeaponID is not null)
            tag["id"] = SoulWeaponID.ToByteArray();
    }

    public override void LoadData(TagCompound tag) => SoulWeaponID = tag.TryGet("id", out byte[] uuid) ? new UUID(uuid) : null;
}

public class SaveSoulWeapons : ModSystem {
    public override void SaveWorldData(TagCompound tag) {
        List<TagCompound> soulWeaponsInWorld = [];
        foreach (Item i in Main.ActiveItems) {
            if (i.ModItem is SoulWeapon) {
                TagCompound t = ItemIO.Save(i);
                t["position"] = i.position;
                soulWeaponsInWorld.Add(t);
            }
        }
        if (soulWeaponsInWorld.Count > 0 && !ModLoader.HasMod("StubbornItems")) // dont want to duplicate items, so dont save soul weapons if a mod that already saves items in world is loaded
            tag["SoulWeapons"] = soulWeaponsInWorld;
    }

    public override void LoadWorldData(TagCompound tag) {
        if (tag.TryGet("SoulWeapons", out TagCompound[] soulWeapons)) {
            foreach (TagCompound t in soulWeapons) {
                if (t.TryGet("position", out Vector2 position)) {
                    try {
                        Item i = ItemIO.Load(t);
                        Item.NewItem(new EntitySource_Misc("SoulWeaponLoaded"), position, i);
                    } catch {
                        Mod.Logger.Warn("Unable to load Soul Weapon at position " + position);
                    }
                }
            }
        }
    }
}