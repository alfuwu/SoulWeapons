using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
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

public class SoulWeapon : ModItem {
    public static Asset<Texture2D>[] meleeSprites, shinyMeleeSprites, yoyoSprites,
        tomeSprites, scepterSprites, staffSprites, whipSprites, pistolSprites, assaultRifleSprites, rifleSprites, bowSprites,
        thrownSprites, pickaxeSprites; // primary textures
    public static Asset<Texture2D>[] handleSprites, tomePatternSprites, staffGemSprites,
        pistolHandleSprites, assaultRifleHandleSprites, rifleHandleSprites; // secondary textures
    public static Asset<Texture2D>[] miscSprites; // tertiary sprites
    public static Asset<Texture2D>[] materials;
    
    public UUID SoulWeaponID { get; set; }
    SoulWeaponType type;
    SubType subType;
    byte[] frame;
    byte[] materialIDs; // 0 is blade/barrel/etc, 1 is handle, 2 is misc textures (sword guard, etc)
    byte stage;
    string name;
    //LocalizedText localizedName = null;

    //public override LocalizedText DisplayName => localizedName ?? base.DisplayName;

    private static Asset<Texture2D> GetFrame(string path) => ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Frames/{path}");
    private static Asset<Texture2D> GetMat(string path) => ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Materials/{path}");

    //public override LocalizedText DisplayName => SoulWeapons.OfLiteral(name);

    public override void Load() {
        // primary sprites
        meleeSprites = [

        ];
        shinyMeleeSprites = [

        ];
        yoyoSprites = [

        ];
        tomeSprites = [

        ];
        scepterSprites = [

        ];
        staffSprites = [

        ];
        whipSprites = [

        ];
        pistolSprites = [

        ];
        assaultRifleSprites = [

        ];
        rifleSprites = [

        ];
        bowSprites = [

        ];
        thrownSprites = [

        ];
        pickaxeSprites = [

        ];

        // secondary sprites
        handleSprites = [

        ];
        tomePatternSprites = [

        ];
        staffGemSprites = [

        ];
        pistolHandleSprites = [

        ];
        assaultRifleHandleSprites = [

        ];
        rifleHandleSprites = [

        ];

        // tertiary sprites
        miscSprites = [

        ];
    }

    public override void Unload() {
        meleeSprites = shinyMeleeSprites = yoyoSprites = tomeSprites = scepterSprites = staffSprites =
            whipSprites = pistolSprites = assaultRifleSprites = rifleSprites = bowSprites =
            thrownSprites = pickaxeSprites = handleSprites = tomePatternSprites = staffGemSprites =
            pistolHandleSprites = assaultRifleHandleSprites = rifleHandleSprites = miscSprites = materials = null;
    }

    public override void SetDefaults() {
        SoulWeaponID = null;
        type = (SoulWeaponType)Main.rand.Next((int)SoulWeaponType.Count);
        int gunType = Main.rand.Next(3);
        if (type == SoulWeaponType.RangedMelee || type == SoulWeaponType.Gun)
            subType = (SubType)(gunType + (type == SoulWeaponType.Gun ? 3 : 0));
        frame = [
            (byte)Main.rand.Next(type switch {
                SoulWeaponType.Melee => meleeSprites.Length,
                SoulWeaponType.RangedMelee => shinyMeleeSprites.Length,
                SoulWeaponType.Yoyo => yoyoSprites.Length,
                SoulWeaponType.Tome => tomeSprites.Length,
                SoulWeaponType.Scepter => scepterSprites.Length,
                SoulWeaponType.Staff => staffSprites.Length,
                SoulWeaponType.Whip => whipSprites.Length,
                SoulWeaponType.Gun => gunType == 0 ? pistolSprites.Length :
                                      gunType == 1 ? assaultRifleSprites.Length :
                                      gunType == 2 ? rifleSprites.Length : -1,
                SoulWeaponType.Bow => bowSprites.Length,
                SoulWeaponType.Thrown => thrownSprites.Length,
                SoulWeaponType.Pickaxe => pickaxeSprites.Length,
                _ => -1
            } + 1),
            (byte)Main.rand.Next(type switch {
                SoulWeaponType.Melee => handleSprites.Length,
                SoulWeaponType.Tome => tomePatternSprites.Length,
                SoulWeaponType.Staff => staffGemSprites.Length,
                SoulWeaponType.Gun => gunType == 0 ? pistolHandleSprites.Length :
                                      gunType == 1 ? assaultRifleHandleSprites.Length :
                                      gunType == 2 ? rifleHandleSprites.Length : -1,
                _ => -1
            } + 1),
            (byte)Main.rand.Next(miscSprites.Length)
        ];
        materialIDs = [(byte)Main.rand.Next(256), (byte)Main.rand.Next(256), (byte)Main.rand.Next(256)];
        stage = 0;
        Item.damage = stage switch {
            0 => Main.rand.Next(10, 25),
            1 => Main.rand.Next(24, 33),
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
        Item.width = 40;
        Item.height = 40;
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.knockBack = 6;
        Item.value = Item.buyPrice(gold: (int)Math.Pow(stage, 1.7), silver: (int)Math.IEEERemainder(Math.Pow(stage, 1.7) * 100, 100));
        Item.rare = stage + 1;
        Item.UseSound = SoundID.Item1;
        Item.SetNameOverride(name);
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        return base.PreDrawInInventory(spriteBatch, position, frame, drawColor, itemColor, origin, scale);
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
        return base.PreDrawInWorld(spriteBatch, lightColor, alphaColor, ref rotation, ref scale, whoAmI);
    }

    public override void NetSend(BinaryWriter writer) {
        writer.Write(SoulWeaponID.ToByteArray());
        writer.Write((byte)type);
        if (type == SoulWeaponType.RangedMelee || type == SoulWeaponType.Gun)
            writer.Write((byte)subType);
        for (int i = 0; i < 3 /*frame.Count*/;  i++)
            writer.Write(frame[i]); // mebbe allow for more frames, but rn that's not needed
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
        frame = reader.ReadBytes(3);
        materialIDs = reader.ReadBytes(3);
        stage = reader.ReadByte();
        name = reader.ReadString();
    }

    public override void SaveData(TagCompound tag) {
        if (SoulWeaponID is not null)
            tag["id"] = SoulWeaponID.ToByteArray();
        tag["type"] = (byte)type;
        if (type == SoulWeaponType.RangedMelee && subType != SubType.Projectile || type == SoulWeaponType.Gun && subType != SubType.Pistol)
            tag["subType"] = (byte)subType;
        tag["frame"] = frame;
        tag["materials"] = materialIDs;
        //if (stage > 0)
            tag["stage"] = stage;
        tag["name"] = name;
        tag["damage"] = Item.damage;
    }

    public override void LoadData(TagCompound tag) {
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
            frame = sid;
        if (tag.TryGet("materials", out byte[] m))
            materialIDs = m;
        if (tag.TryGet("name", out string n))
            name = n;
        if (tag.TryGet("stage", out byte s))
            stage = s;
        Item.damage = tag.TryGet("damage", out int dmg) ? dmg : 1;
        Init(); // is there a better way to do this?
    }

    /*public override ModItem Clone(Item newEntity) {
        SoulWeapon clone = (SoulWeapon)base.Clone(newEntity);
        clone.SoulWeaponID = SoulWeaponID;
        clone.type = type;
        clone.frame = frame;
        clone.materialIDs = materialIDs;
        clone.stage = stage;
        return clone;
    }*/

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
