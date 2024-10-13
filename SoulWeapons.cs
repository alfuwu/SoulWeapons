using MonoMod.RuntimeDetour;
using SoulWeapons.Content.Items;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace SoulWeapons;

public class SoulWeapons : Mod {
    public const string Localization = $"Mods.{nameof(SoulWeapons)}";

    //public static LocalizedText OfLiteral(string literal) => (LocalizedText)typeof(LocalizedText).TypeInitializer.Invoke(["Mods.SoulWeapons.Items.SoulWeapon.DisplayName", literal]);
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