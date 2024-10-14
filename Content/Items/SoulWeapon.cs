using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulWeapons.Content.Items;

public enum SoulWeaponType : byte {
    Melee,
    RangedMelee,
    Yoyo,
    Tome,
    Scepter,
    Staff,
    Whip,
    Gun,
    Bow,
    Thrown,
    Pickaxe,
    Count
}

public enum SubType : byte {
    Projectile,
    FancySlash,
    FancySlashANDProjectile,
    Pistol,
    AssaultRifle,
    Rifle
}

public struct Frame(Asset<Texture2D> texture, int x, int y) {
    public Asset<Texture2D> texture = texture;
    public int x = x; // size x of the image
    public int y = y; // size y of the image

    public static bool operator ==(Frame left, Frame right) => left.texture == right.texture && left.x == right.x && left.y == right.y;
    public static bool operator !=(Frame left, Frame right) => left.texture != right.texture && left.x != right.x && left.y != right.y;
}

public class SoulWeapon : ModItem {
    internal static Frame[] meleeFrames, shinyMeleeFrames, yoyoFrames,
        tomeFrames, scepterFrames, staffFrames, whipFrames, pistolFrames, assaultRifleFrames, rifleFrames, bowFrames,
        thrownFrames, pickaxeFrames; // primary textures
    internal static Frame[] handleFrames, yoyoPatternFrames, tomePatternFrames,
        staffGemFrames, pistolHandleFrames, assaultRifleHandleFrames, rifleHandleFrames, pickaxeHandleFrames; // secondary textures
    internal static Frame[] miscFrames; // tertiary sprites
    internal static Asset<Texture2D>[] materials;
    
    public UUID SoulWeaponID { get; set; }
    SoulWeaponType type;
    SubType subType;
    Frame[] frame;
    byte[] materialIDs; // 0 is blade/barrel/etc, 1 is handle, 2 is misc textures (sword guard, etc)
    byte stage;
    string name;

    private static Frame GetFrames(string path, int x, int y) => new(ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Frames/{path}"), x, y);
    private static Asset<Texture2D> GetMat(string path) => ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Materials/{path}");

    public override void Load() {
        // primary sprites
        meleeFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        shinyMeleeFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        yoyoFrames = [
            GetFrames("Yoyo_1", 30, 26),
        ];
        tomeFrames = [
            GetFrames("Tome_1", 28, 32),
        ];
        scepterFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        staffFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        whipFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        pistolFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        assaultRifleFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        rifleFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        bowFrames = [
            GetFrames("Sword_1", 30, 38),
        ];
        thrownFrames = [
            GetFrames("Dagger_1", 10, 24),
        ];
        pickaxeFrames = [
            GetFrames("Sword_1", 30, 38),
        ];

        // secondary sprites
        handleFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        yoyoPatternFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        tomePatternFrames = [
            GetFrames("TomePattern_1", 0, 0),
        ];
        staffGemFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        pistolHandleFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        assaultRifleHandleFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        rifleHandleFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];
        pickaxeHandleFrames = [
            GetFrames("SwordHandle_1", 16, 16),
        ];

        // tertiary sprites
        miscFrames = [
            new Frame()
        ];
    }

    public override void Unload() {
        meleeFrames = shinyMeleeFrames = yoyoFrames = tomeFrames = scepterFrames = staffFrames =
            whipFrames = pistolFrames = assaultRifleFrames = rifleFrames = bowFrames =
            thrownFrames = pickaxeFrames = handleFrames = tomePatternFrames = staffGemFrames =
            pistolHandleFrames = assaultRifleHandleFrames = rifleHandleFrames = miscFrames = null;
        materials = null;
    }

    public override void SetDefaults() {
        SoulWeaponID = null;
        type = (SoulWeaponType)Main.rand.Next((int)SoulWeaponType.Count);
        int gunType = Main.rand.Next(3);
        if (type == SoulWeaponType.RangedMelee || type == SoulWeaponType.Gun)
            subType = (SubType)(gunType + (type == SoulWeaponType.Gun ? 3 : 0));
        Frame[] a = GetPrimaryFrameArray();
        Frame[] b = GetSecondaryFrameArray();
        frame = [
            a.Length > 0 ? Main.rand.NextFromList(a) : new Frame(),
            b.Length > 0 ? Main.rand.NextFromList(b) : new Frame(),
            Main.rand.NextFromList(miscFrames)
        ];
        materialIDs = [(byte)Main.rand.Next(256), (byte)Main.rand.Next(256), (byte)Main.rand.Next(256)];
        stage = 1;
        Item.damage = stage switch {
            0 => Main.rand.Next(10, 25),
            1 => Main.rand.Next(25, 33),
            2 => Main.rand.Next(33, 39), // early hardmode
            3 => Main.rand.Next(39, 45),
            4 => Main.rand.Next(45, 61), // post mech
            5 => Main.rand.Next(61, 71),
            6 => Main.rand.Next(71, 83),
            7 => Main.rand.Next(83, 105), // post golem
            8 => Main.rand.Next(105, 151), // end game
            9 => Main.rand.Next(151, 401), // post ml
            _ => 401 // beyond post ml (unobtainable)
        };
        name = Main.rand.NextFromList<string>(type switch {
            SoulWeaponType.Melee or SoulWeaponType.RangedMelee => [ "Throngler", "Flashy Sword Name" ],
            SoulWeaponType.Yoyo => [ "Yoyo" ],
            SoulWeaponType.Tome => [ "Tome" ],
            SoulWeaponType.Scepter or SoulWeaponType.Staff => [ "Scepter" ],
            SoulWeaponType.Whip => [ "Whip" ],
            SoulWeaponType.Gun => [ "Gun" ],
            SoulWeaponType.Bow => [ "Bow" ],
            SoulWeaponType.Thrown => [ "Thrown" ],
            SoulWeaponType.Pickaxe => [ "Pickaxe" ],
            _ => [ "???" ]
        });
        Init();
    }

    public void Init() {
        switch (type) {
            case SoulWeaponType.Melee:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Swing;
                break;
            case SoulWeaponType.RangedMelee:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Swing;
                break;
            case SoulWeaponType.Yoyo:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Shoot;
                break;
            case SoulWeaponType.Tome:
                Item.DamageType = DamageClass.Magic;
                Item.useStyle = ItemUseStyleID.Shoot;
                break;
            case SoulWeaponType.Scepter:
                Item.DamageType = DamageClass.Magic;
                Item.staff[Type] = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                break;
            case SoulWeaponType.Staff:
                Item.DamageType = DamageClass.Magic;
                Item.staff[Type] = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                break;
            case SoulWeaponType.Whip:
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.useStyle = ItemUseStyleID.Swing;
                break;
            case SoulWeaponType.Gun:
                Item.DamageType = DamageClass.Ranged;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ItemID.PurificationPowder;
                Item.useAmmo = AmmoID.Bullet;
                break;
            case SoulWeaponType.Bow:
                Item.DamageType = DamageClass.Ranged;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ItemID.PurificationPowder;
                Item.useAmmo = AmmoID.Arrow;
                break;
            case SoulWeaponType.Thrown:
                Item.DamageType = DamageClass.Ranged;
                break;
            case SoulWeaponType.Pickaxe:
                Item.DamageType = DamageClass.Melee;
                Item.autoReuse = true;
                break;
            default:
                Item.DamageType = DamageClass.Default;
                Item.damage = 1;
                break;
        }
        Item.width = 0;
        Item.height = 0;
        for (int i = 0; i < frame.Length; i++) {
            if (frame[i].texture != null) {
                Item.width += frame[i].texture.Value.Width;
                Item.height += frame[i].texture.Value.Height;
            }
        }
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.knockBack = 6;
        Item.value = Item.sellPrice(gold: (int)Math.Pow(stage + 1, 1.7), silver: (int)Math.IEEERemainder(Math.Pow(stage + 1, 1.7) * 100, 100));
        Item.rare = stage + 1;
        Item.UseSound = SoundID.Item1;
        Item.SetNameOverride(name);
        Item.NetStateChanged();
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle f, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        //f = new Rectangle(0, 0, (int)size.X, (int)size.Y);
        Mod.Logger.Info(scale);
        position += new Vector2(0, Item.height * (scale / 2));
        for (int i = frame.Length - 1; i > -1; i--) {
            if (frame[i].texture != null) { // how to dynamic render
                position -= new Vector2(0, frame[i].texture.Value.Height / 2f * scale);
                spriteBatch.Draw(frame[i].texture.Value, position, new Rectangle(0, 0, frame[i].texture.Value.Width, frame[i].texture.Value.Height), drawColor, 0, origin, scale, SpriteEffects.None, 0);
                position += new Vector2(frame[i].x, -frame[i].y) * scale;
            }
        }
        return false;
        //return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
        /*position += new Vector2(0, Item.height / 2f);
        for (int i = frame.Length - 1; i > -1; i--) {
            if (frame[i].texture != null) { // how to dynamic render
                position -= new Vector2(0, frame[i].texture.Value.Height * scale) / 2;
                spriteBatch.Draw(frame[i].texture.Value, position, new Rectangle(0, 0, frame[i].texture.Value.Width, frame[i].texture.Value.Height), drawColor, 0, origin, scale, SpriteEffects.None, 0);
                position += new Vector2(frame[i].x, -frame[i].y) * scale;
            }
        }
        return false;*/
        return true;
    }

    public Frame[] GetPrimaryFrameArray() {
        return type switch {
            SoulWeaponType.Melee => meleeFrames,
            SoulWeaponType.RangedMelee => shinyMeleeFrames,
            SoulWeaponType.Yoyo => yoyoFrames,
            SoulWeaponType.Tome => tomeFrames,
            SoulWeaponType.Scepter => scepterFrames,
            SoulWeaponType.Staff => staffFrames,
            SoulWeaponType.Whip => whipFrames,
            SoulWeaponType.Gun => subType == SubType.Pistol ? pistolFrames :
                                  subType == SubType.AssaultRifle ? assaultRifleFrames :
                                  subType == SubType.Rifle ? rifleFrames : [],
            SoulWeaponType.Bow => bowFrames,
            SoulWeaponType.Thrown => thrownFrames,
            SoulWeaponType.Pickaxe => pickaxeFrames,
            _ => []
        };
    }

    public Frame[] GetSecondaryFrameArray() {
        return type switch {
            SoulWeaponType.Melee => handleFrames,
            SoulWeaponType.Tome => tomePatternFrames,
            SoulWeaponType.Staff => staffGemFrames,
            SoulWeaponType.Gun => subType == SubType.Pistol ? pistolHandleFrames :
                                  subType == SubType.AssaultRifle ? assaultRifleHandleFrames :
                                  subType == SubType.Rifle ? rifleHandleFrames : [],
            _ => []
        };
    }

    public override void NetSend(BinaryWriter writer) {
        writer.Write(SoulWeaponID.ToByteArray());
        writer.Write((byte)type);
        if (type == SoulWeaponType.RangedMelee || type == SoulWeaponType.Gun)
            writer.Write((byte)subType);
        for (int i = 0; i < 3 /*frame.Count*/; i++)
            writer.Write(frame[i].texture != null ? i switch {
                0 => Array.FindIndex(GetPrimaryFrameArray(), f => f == frame[i]) + 1, // mebbe allow for more frames, but rn that's not needed
                1 => Array.FindIndex(GetSecondaryFrameArray(), f => f == frame[i]) + 1,
                2 => Array.FindIndex(miscFrames, f => f == frame[i]) + 1,
                _ => 0
            } : 0);
        for (int i = 0; i < 3; i++)
            writer.Write(materialIDs[i]);
        writer.Write(stage);
        writer.Write(name);
    }

    public override void NetReceive(BinaryReader reader) {
        SoulWeaponID = new(reader.ReadBytes(16));
        type = (SoulWeaponType)reader.ReadByte();
        if (type == SoulWeaponType.RangedMelee || type == SoulWeaponType.Gun)
            subType = (SubType)reader.ReadByte();
        byte[] frames = reader.ReadBytes(3);
        frame = [frames[0] > 0 ? GetPrimaryFrameArray()[frames[0] - 1] : new Frame(),
            frames[1] > 0 ? GetSecondaryFrameArray()[frames[1] - 1] : new Frame(),
            frames[2] > 0 ? miscFrames[frames[2] - 1] : new Frame()];
        materialIDs = reader.ReadBytes(3);
        stage = reader.ReadByte();
        name = reader.ReadString();
    }

    public void Reset() {
        Item.crit = 0;
        Item.reuseDelay = 0;
        Item.consumeAmmoOnFirstShotOnly = false;
        Item.consumeAmmoOnLastShotOnly = false;
        Item.InterruptChannelOnHurt = false;
        Item.StopAnimationOnHurt = false;
        Item.DamageType = DamageClass.Default;
        Item.ChangePlayerDirectionOnShoot = true;
        Item.ArmorPenetration = 0;
        Item.material = false;
        Item.mana = 0;
        Item.channel = false;
        Item.manaIncrease = 0;
        Item.noMelee = false;
        Item.noUseGraphic = false;
        Item.lifeRegen = 0;
        Item.shoot = ItemID.None;
        Item.shootSpeed = 0f;
        Item.alpha = 0;
        Item.ammo = AmmoID.None;
        Item.useAmmo = AmmoID.None;
        Item.autoReuse = false;
        Item.axe = 0;
        Item.healMana = 0;
        Item.potion = false;
        Item.color = default;
        Item.glowMask = -1;
        Item.consumable = false;
        Item.damage = -1;
        Item.hammer = 0;
        Item.healLife = 0;
        Item.holdStyle = 0;
        Item.knockBack = 0f;
        Item.pick = 0;
        Item.rare = 0;
        Item.scale = 1f;
        Item.shoot = 0;
        Item.tileBoost = 0;
        Item.useStyle = 0;
        Item.UseSound = null;
        Item.useTime = 100;
        Item.useAnimation = 100;
        Item.value = 0;
        Item.useTurn = false;
        Item.buy = false;
        Item.shootsEveryUse = false;
    }

    public override void SaveData(TagCompound tag) {
        if (SoulWeaponID is not null)
            tag["id"] = SoulWeaponID.ToByteArray();
        tag["type"] = (byte)type;
        if (type == SoulWeaponType.RangedMelee && subType != SubType.Projectile || type == SoulWeaponType.Gun && subType != SubType.Pistol)
            tag["subType"] = (byte)subType;
        byte[] frames = new byte[frame.Length];
        for (int i = 0; i < frames.Length; i++)
            frames[i] = (byte)(frame[i].texture != null ? i switch {
                0 => Array.FindIndex(GetPrimaryFrameArray(), f => f == frame[i]) + 1,
                1 => Array.FindIndex(GetSecondaryFrameArray(), f => f == frame[i]) + 1,
                2 => Array.FindIndex(miscFrames, f => f == frame[i]) + 1,
                _ => 0
            } : 0);
        tag["frame"] = frames;
        tag["materials"] = materialIDs;
        //if (stage > 0)
            tag["stage"] = stage;
        tag["name"] = name;
        tag["damage"] = Item.damage;
    }

    public override void LoadData(TagCompound tag) {
        Reset(); // remove any modifications to stats done in SetDefaults
        if (tag.TryGet("id", out byte[] id))
            SoulWeaponID = new UUID(id);
        if (tag.TryGet("type", out byte t))
            type = (SoulWeaponType)t;
        if (type == SoulWeaponType.Melee || type == SoulWeaponType.Gun)
            if (tag.TryGet("subType", out byte st))
                subType = (SubType)st;
            else if (type == SoulWeaponType.Melee)
                subType = SubType.Projectile;
            else if (type == SoulWeaponType.Gun)
                subType = SubType.Pistol;
        if (tag.TryGet("frame", out byte[] sid))
            frame = [sid[0] > 0 ? GetPrimaryFrameArray()[sid[0] - 1] : new Frame(),
                sid[1] > 0 ? GetSecondaryFrameArray()[sid[1] - 1] : new Frame(),
                sid[2] > 0 ? miscFrames[sid[2] - 1] : new Frame()];
        if (tag.TryGet("materials", out byte[] m))
            materialIDs = m;
        if (tag.TryGet("name", out string n))
            name = n;
        if (tag.TryGet("stage", out byte s))
            stage = s;
        Item.damage = tag.TryGet("damage", out int dmg) ? dmg : 1;
        Init();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips) {
        UUID uuid = Main.LocalPlayer.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID;
        if (SoulWeaponID is null && uuid is null)
            tooltips.Add(new(Mod, "UnboundSoulWeapon", Language.GetTextValue($"{SoulWeapons.Localization}.SoulWeapon.Unbound")));
        else if (!CanPlayerWield(Main.LocalPlayer))
            tooltips.Add(new(Mod, "IncompatibleSoulWeapon", Language.GetTextValue($"{SoulWeapons.Localization}.SoulWeapon.Incompatible")));
        else
            tooltips.Add(new(Mod, "BoundSoulWeapon", Language.GetTextValue($"{SoulWeapons.Localization}.SoulWeapon.Bound")));
    }

    public override void UpdateInventory(Player player) {
        if (!CanPlayerWield(player))
            player.DropItem(player.GetSource_FromThis("IncompatibleSoulWeapon"), player.Center, ref Unsafe.AsRef(Item));
    }

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
        UUID uuid = player.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID;
        if (uuid is null && SoulWeaponID is null || uuid is not null && SoulWeaponID is null || SoulWeaponID is null && uuid is not null) // weapon is unbound/somehow the player has the item in their inventory despite it belonging to another player
            damage /= 2; // give a harsh penalty to the weapon's damage if its unbound, to discourage using unbound weapons
    }

    public override bool CanPickup(Player player) => CanPlayerWield(player);

    public bool CanPlayerWield(Player player) {
        UUID uuid = player.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID;
        return uuid is null && SoulWeaponID is null || uuid is not null && SoulWeaponID is not null && uuid == SoulWeaponID;
    }

    public override void AddRecipes() {
        /*CreateRecipe()
            .AddIngredient(ItemID.DirtBlock, 10)
            .AddTile(TileID.WorkBenches)
            .Register();*/
        // uncraftable (workbenches dont like dynamic items)
    }
}
