using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SoulWeapons.Content.Items;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace SoulWeapons;

public class SoulWeapons : Mod {
    public const string Localization = $"Mods.{nameof(SoulWeapons)}";

    public override void Load() {
        IL_ItemSlot.DrawItemIcon += DrawItemIcon;
        IL_PlayerDrawLayers.DrawPlayer_27_HeldItem += DrawPlayer_27_HeldItem;
        IL_Player.ItemCheck_ApplyUseStyle_Inner += ItemCheck_ApplyUseStyle_Inner;
    }

    public override void Unload() {
        IL_ItemSlot.DrawItemIcon -= DrawItemIcon;
        IL_PlayerDrawLayers.DrawPlayer_27_HeldItem -= DrawPlayer_27_HeldItem;
        IL_Player.ItemCheck_ApplyUseStyle_Inner -= ItemCheck_ApplyUseStyle_Inner;
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
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon s && (s.type == SoulWeaponType.Scepter || s.type == SoulWeaponType.Staff));
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
            c.EmitDelegate((Item item) => item.ModItem is SoulWeapon s && (s.type == SoulWeaponType.Scepter || s.type == SoulWeaponType.Staff));
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