using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulWeapons.Content.Projectiles;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SoulWeapons.Content.Items;

public enum WeaponType : byte {
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

public struct Frame(Asset<Texture2D> texture, byte x, byte y, sbyte offsetX = 0, sbyte offsetY = 0) {
    public Asset<Texture2D> texture = texture;
    public byte x = x; // size x of the image
    public byte y = y; // size y of the image
    public sbyte offsetX = offsetX;
    public sbyte offsetY = offsetY;

    public override readonly bool Equals([NotNullWhen(true)] object obj) => obj is Frame f && this == f;
    public override readonly int GetHashCode() => base.GetHashCode();

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
    internal static (Asset<Texture2D> material, Color color)[] materials;
    internal static (float weight, Action<Item, SoulWeapon, bool> modifier, Predicate<SoulWeapon> apply)[] modifiers;

    delegate void ModifyHit(Player player, NPC npc, ref NPC.HitModifiers hitInfo);
    delegate void ModifyHitPlayer(Player player, Player other, ref Player.HurtModifiers hitInfo);
    delegate void AfterHit(Player player, NPC npc, NPC.HitInfo hit, int damageDone);
    delegate void AfterHitPlayer(Player player, Player other, Player.HurtInfo hitInfo, int damageDone);
    delegate void ModifyShoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback);

    [field: CloneByReference]
    public UUID SoulWeaponID { get; set; }
    public WeaponType type;
    public SubType subType;
    [CloneByReference]
    Frame[] frame;
    [CloneByReference]
    public byte[] materialIDs; // 0 is blade/barrel/etc, 1 is handle, 2 is misc textures (sword guard, etc)
    [CloneByReference]
    byte[] modifierIDs;
    [CloneByReference]
    byte[] weaponStats; // custom weapon stats (shoot count, yoyo length/duration, etc)
    event ModifyHit OnHit;
    event ModifyHitPlayer OnHitPlayer;
    event AfterHit PostHit;
    event AfterHitPlayer PostHitPlayer;
    event ModifyShoot OnShoot;
    short dust;
    byte stage;
    string name;
    [CloneByReference]
    public Texture2D texture;

    private static Frame GetFrames(string path, byte x, byte y, sbyte offsetX = 0, sbyte offsetY = 0) => new(ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Frames/{path}"), x, y, offsetX, offsetY);
    private static Asset<Texture2D> GetMat(string path) => ModContent.Request<Texture2D>($"{nameof(SoulWeapons)}/Content/Materials/{path}");

    const string consonants = "bcdfghjklmnprstvwz";
    const string vowels = "aeiou";

    public static string GenerateName() {
        // create arrays within method to save on memory usage; they'll be gc'd when no longer in use
        // increases the processing usage significantly, but this function isn't supposed to be called very often anyways
        string[] consonantClusters = ["st", "th", "ch", "sh", "ph", "tr", "dr", "cl", "br", "cr", "gr", "fr", "bl", "gl"];
        string[] commonSyllables = ["ar", "el", "an", "en", "er", "or", "al", "in", "on", "ir", "il", "us", "ur", "is"];
        string[] prefixes = ["Ex", "Al", "Gr", "Ze", "Ka", "Th", "Ari", "Fi", "Val", "For", "Gal", "Gil"];
        string[] suffixes = ["dor", "mir", "mar", "ros", "gon", "lore", "bur", "din", "or"];
        string[] badClusters = ["wvr", "rvw", "vwr", "vrw", "rwv", "pq", "qp", "xk", "kx", "wx", "xw", "vrz", "rvz", "rzv", "zrv", "zvr", "vzr",
            "ouae", "eai", "aio", "oie", "iua", "iae", "iau"]; // permutations are hardcoded because yes
        string[] badWords = ["fuck", "shit", "dick", "penis", "vagina", "nazi", new string([(char)114, (char)101, (char)116, (char)97, (char)114, (char)100]), new string([(char)110, (char)105, (char)103, (char)103]) /* yes i'm not typing this out */];
        Regex BadConsonantRegex = new(@"([^aeiou])[^aeiou]+\1|[^aeiou]{4,}|q[^u]|[^aeiou]+w[^aeiouh]+|[^aeioutscklprymn]+t[^aeiouhtrl]+|[^aeiou]+r[^aeiou]+|([^aeiou])\2{2,}"); // matches "unenglishy" consonant clusters, which will get replaced with a single consonant instead
        // this regex matches:
        // consonant clusters with a repeating consonant (e.g. zrz)
        // consonant clusters with four or more consonants in it (e.g. sdfg)
        // the consonant q when not followed by a u (e.g. qa)
        // the consonant w when not followed by a vowel or the consonant h, or when it has consonants behind it (e.g. wt or hw)
        // the consonant t when not followed by an h, a t, an r, or an l, or when it has a consonant that doesn't mesh with it very well (e.g. tj or bth)
        // the consonant r when it is preceeded by a consonant and succeeded by a consonant (e.g. frg)
        // any cluster of three or more of the same consonant (e.g. bbb)

        // add a renaming system so players can name their weapons what they want if they dislike the random name (which will probably be quite a common occurence if i'm bein honest here)
        // ^^^ maybe not, to make the good names actually valuable?
        // also add a config option that makes names be chosen from a static list if people prefer more "handpicked" names
        string name = "";

        char RandomConsonant() {
            char c = consonants[Main.rand.Next(18)];
            while (c is 'x' or 'w' && name.Contains(c))
                c = consonants[Main.rand.Next(18)];
            if (name.Length <= 0 && c == 'p')
                c = consonants[Main.rand.Next(18)];
            foreach (string bad in badWords)
                while ((name + c).Contains(bad))
                    c = RandomConsonant();
            return c;
        }

        char RandomVowel() {
            char v = vowels[Main.rand.Next(5)];
            for (int i = 0; i < 3; i++)
                if (name.Length > 0 && (v == name[^1] || name[^1] == 'u' && v == 'e' && name.Length > 3) || name.Length <= 0 && v == 'u') // u is a bad vowel to start names with
                    v = vowels[Main.rand.Next(5)];
            foreach (string bad in badWords)
                while ((name + v).Contains(bad))
                    v = RandomVowel();
            return v;
        }

        string RandomConsonantCluster() => consonantClusters[Main.rand.Next(14)];

        string RandomSyllable() => commonSyllables[Main.rand.Next(13)];

        void GenerateSyllable(byte structure) {
            switch (structure) {
                case 0:
                    name += RandomConsonant();
                    name += RandomVowel();
                    name += RandomConsonant();
                    break;
                case 1:
                    name += RandomVowel();
                    name += RandomConsonant();
                    break;
                case 2:
                    name += RandomConsonant();
                    name += RandomVowel();
                    break;
                case 3:
                    name += RandomVowel();
                    name += RandomConsonant();
                    name += RandomVowel();
                    break;
                case 4:
                    name += RandomConsonantCluster();
                    name += RandomVowel();
                    break;
                default:
                    break;
            }
        }

        void Japaneseify() {
            string[] romajiMap = ["a", "bu", "ku", "do", "e", "fu", "gu", "ha", "i", "ju", "ku", "ru", "mu", "n", "o", "pu", "ku", "ru", "su", "to", "u", "bu", "wa", "ku", "ya", "zu"];
            (string romaji, string katakana)[] katakanaMap = [
                ("kya", "キャ"), ("kyu", "キュ"), ("kyo", "キョ"),
                ("sha", "シャ"), ("shu", "シュ"), ("sho", "ショ"),
                ("cha", "チャ"), ("chu", "チュ"), ("cho", "チョ"),
                ("ja", "ジャ"), ("ju", "ジュ"), ("jo", "ジョ"),
                ("nya", "ニャ"), ("nyu", "ニュ"), ("nyo", "ニョ"),
                ("hya", "ヒャ"), ("hyu", "ヒュ"), ("hyo", "ヒョ"),
                ("mya", "ミャ"), ("myu", "ミュ"), ("myo", "ミョ"),
                ("rya", "リャ"), ("ryu", "リュ"), ("ryo", "リョ"),
                ("aa", "アー"), ("ii", "イー"), ("uu", "ウー"), ("ee", "エー"), ("oo", "オー"),
                ("fu", "フ"), ("th", "テ"), ("ph", "フ"),
                ("sa", "サ"), ("shi", "シ"), ("su", "ス"), ("se", "セ"), ("so", "ソ"),
                ("za", "ザ"), ("ji", "ジ"), ("zu", "ズ"), ("ze", "ゼ"), ("zo", "ゾ"),
                ("ka", "カ"), ("ki", "キ"), ("ku", "ク"), ("ke", "ケ"), ("ko", "コ"),
                ("ga", "ガ"), ("gi", "ギ"), ("gu", "グ"), ("ge", "ゲ"), ("go", "ゴ"),
                ("ta", "タ"), ("chi", "チ"), ("tsu", "ツ"), ("te", "テ"), ("to", "ト"),
                ("ra", "ラ"), ("ri", "リ"), ("ru", "ル"), ("re", "レ"), ("ro", "ロ"),
                ("na", "ナ"), ("ni", "ニ"), ("nu", "ヌ"), ("ne", "ネ"), ("no", "ノ"),
                ("ma", "マ"), ("mi", "ミ"), ("mu", "ム"), ("me", "メ"), ("mo", "モ"),
                ("ha", "ハ"), ("hi", "ヒ"), ("hu", "フ"), ("he", "ヘ"), ("ho", "ホ"),
                ("ba", "バ"), ("bi", "ビ"), ("bu", "ブ"), ("be", "ベ"), ("bo", "ボ"),
                ("pa", "パ"), ("pi", "ピ"), ("pu", "プ"), ("pe", "ペ"), ("po", "ポ"),
                ("wa", "ワ"), ("wo", "ヲ"),
                ("a", "ア"), ("i", "イ"), ("u", "ウ"), ("e", "エ"), ("o", "オ"),
                ("n", "ン"), ("r", "ル"), ("m", "ン")
            ];
            const string specialVowels = "auo";
            string romajiName = "";
            Regex YFix = new(@"y(i|e)");
            Regex JFix = new(@"j(e)");
            Regex WFix = new(@"w(i|u)");
            Regex LFix = new(@"l+");
            Regex RemoveInvalidCharacters = new(@"ch([^aeiou])");
            name = YFix.Replace(
                JFix.Replace(
                WFix.Replace(
                LFix.Replace(
                RemoveInvalidCharacters.Replace(name.Replace("kh", "k"),
                match => "ch" + match.Groups[1]), "l"), match => "r" + match.Groups[1]), match => "ju"), match => "ya");

            for (int i = 0; i < name.Length;) { // this sucks
                char c = name[i];

                if (i + 2 < name.Length && name[i..(i + 3)] == "tsu" || i + 1 < name.Length && name[i..(i + 2)] == "tu") {
                    romajiName += "tsu";
                    i += name[i + 1] == 'u' ? 2 : 3;
                    continue;
                }

                if (c - 97 >= 0) {
                    if (i + 1 < name.Length && (vowels.Contains(name[i + 1]) || (c == 'c' || c == 's') && name[i + 1] == 'h')) {
                        if ((c == 't' || c == 's') && name[i + 1] == 'i') {
                            if (i + 2 < name.Length && specialVowels.Contains(name[i + 2]))
                                romajiName += (c == 't' ? "ch" : "sh") + name[i + 2];
                            else
                                romajiName += c == 't' ? "chi" : "shi";
                            i += 2;
                        } else {
                            romajiName += c == 'c' ? 'c' : romajiMap[c - 97][0];
                        }
                    } else if (c != 'h' || i < 1 || name[i - 1] != 'c' && name[i - 1] != 's') {
                        romajiName += romajiMap[c - 97];
                    }
                } else {
                    romajiName += c;
                }
                i++;
            }

            foreach ((string romaji, string katakana) in katakanaMap)
                romajiName = romajiName.Replace(romaji, katakana); // not really romaji anymore is it?
            name = romajiName;
        }

        string PostProcessName() {
            // also possibly add a system to break up most vowel clusters, as they're quite common
            foreach (string bad in badWords) // prevent bad words (very unrefined filter, but it gets its job done more or less)
                if (name.Contains(bad))
                    name = name.Replace(bad, Main.rand.NextDouble() < 0.5 ? RandomVowel().ToString() : RandomConsonant().ToString());
            foreach (string bad in badClusters)
                if (name.Contains(bad))
                    name = name.Replace(bad, RandomConsonant().ToString());
            // this regex makes both t & w significantly rarer due to the way it replaces alternatives 4 & 5
            // maybe find a better way to replace?
            name = BadConsonantRegex.Replace(name, match => RandomConsonant().ToString());
            if (Main.rand.NextDouble() < 0.01) { // rarely generate a second name
                string secondName = GenerateName();
                name += (Main.rand.NextDouble() > 0.2 || secondName.Contains(' ') ? ' ' : '-') + secondName;
            }
            if (name.Contains("ae") && Main.rand.NextDouble() < 0.1)
                name = name.Replace("ae", "æ");
            if (Main.rand.NextDouble() < 6.103515625e-05)
                Japaneseify();
            return name[..1].ToUpper() + name[1..];
        }

        if (Main.rand.NextDouble() < 0.05)
            name += prefixes[Main.rand.Next(12)];

        int j = (Main.rand.NextDouble() < 0.1 ? Main.rand.Next(2) : 0) + 1;
        for (int i = 0; i < j; i++)
            GenerateSyllable((byte)Main.rand.Next(5));
        if (Main.rand.NextDouble() < 0.975)
            name += RandomSyllable();
        if (Main.rand.NextDouble() < 0.03)
            name += suffixes[Main.rand.Next(9)];

        return PostProcessName();
    }

    public override void Load() {
        // primary sprites
        meleeFrames = [
            GetFrames("Sword_1", 30, 38),
            GetFrames("Sword_2", 60, 60, -6, -6),
            GetFrames("Sword_3", 64, 64, -4, -4),
            GetFrames("Sword_4", 42, 46, -2, -2),
            GetFrames("Sword_5", 20, 20, -2, -2),
            GetFrames("Sword_6", 44, 44, -4, -4),
            GetFrames("Sword_7", 44, 44, -8, -8),
            GetFrames("Sword_8", 48, 48, -6, -6),
            GetFrames("Sword_9", 38, 62, -2, -2),
            GetFrames("Sword_10", 48, 48, -2, -2),
        ];
        shinyMeleeFrames = [
            ..meleeFrames
        ];
        yoyoFrames = [
            GetFrames("Yoyo_1", 30, 26),
        ];
        tomeFrames = [
            GetFrames("Tome_1", 0, 0),
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
            GetFrames("SwordHandle_1", 14, 14),
            GetFrames("SwordHandle_2", 14, 14),
            GetFrames("SwordHandle_3", 14, 14),
            GetFrames("SwordHandle_4", 14, 14),
            GetFrames("SwordHandle_5", 14, 14),
            GetFrames("SwordHandle_6", 20, 20),
            GetFrames("SwordHandle_7", 14, 14),
        ];
        yoyoPatternFrames = [
            GetFrames("SwordHandle_1", 14, 14),
        ];
        tomePatternFrames = [
            GetFrames("TomePattern_1", 28, 32),
        ];
        staffGemFrames = [
            GetFrames("SwordHandle_1", 14, 14),
        ];
        pistolHandleFrames = [
            GetFrames("PistolHandle_1", 12, 10),
        ];
        assaultRifleHandleFrames = [
            GetFrames("SwordHandle_1", 14, 14),
        ];
        rifleHandleFrames = [
            GetFrames("SwordHandle_1", 14, 14),
        ];
        pickaxeHandleFrames = [
            GetFrames("SwordHandle_1", 14, 14),
        ];

        // tertiary sprites
        miscFrames = [
            new Frame()
        ];

        materials = [
            (GetMat("0"), Color.Pink),
            (GetMat("1"), Color.DarkSlateGray)
        ];

        modifiers = [
            (0.5f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                if (weapon.stage == 0) {
                    item.useTime += 10; // slows attack speed by increasing use time
                    if (weapon.type >= WeaponType.Melee && weapon.type <= WeaponType.Yoyo || weapon.type == WeaponType.Pickaxe)
                        item.knockBack *= 1.5f; // increase knockback for melee weapons

                    item.scale *= 1.4f;
                    if (weapon.type == WeaponType.Yoyo) {
                        // reduce yoyo length and duration
                        weapon.weaponStats[2] = (byte)Math.Max(weapon.weaponStats[2] - 10, 1); // yoyo length
                        weapon.weaponStats[3] = (byte)Math.Max(weapon.weaponStats[3] - 10, 1); // yoyo duration
                    }
                }
                item.damage = (int)(item.damage * 1.2f);
                if (weapon.type == WeaponType.Pickaxe)
                    item.pick = (int)(item.pick * 1.3f);
            }, weapon => true),
            (0.4f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                if (weapon.stage == 0)
                    item.useTime = (int)(item.useTime * 0.7f);
                item.damage = (int)(item.damage * 0.8f);
                if (weapon.type >= WeaponType.Gun && weapon.type <= WeaponType.Thrown) {
                    item.shootSpeed += 1;
                    weapon.weaponStats[3] = (byte)Math.Max(weapon.weaponStats[3] - 5, 0); // ammo consumption
                }
            }, weapon => true),
            (0.3f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 1.1f);
                item.useTime += 5;
                weapon.weaponStats[0] |= 0b00000001; // aoe
            }, weapon => true),
            (0.3f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                weapon.weaponStats[2] += 1; // adds 1 to piercing count for projectiles
                item.damage = (int)(item.damage * (weapon.stage == 0 ? 0.9f : 0.95f));
                item.shootSpeed += 1;
            }, weapon => weapon.type >= WeaponType.Tome && weapon.type <= WeaponType.Thrown && weapon.type != WeaponType.Whip),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * (weapon.stage == 0 ? 1.2f : 1.05f));
                if (weapon.stage == 0)
                    item.defense -= 5;
            }, weapon => true),
            (0.1f, (item, weapon, dryRun) => { // freezing
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    npc.AddBuff(BuffID.Frozen, 60);
                if (dryRun)
                    return;
                item.useTime += 3;
                item.crit += 5;
            }, weapon => weapon.stage == 0),
            (0.1f, (item, weapon, dryRun) => { // lightning
                // implement lighting
                //weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                //    
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 0.9f);
                item.useTime -= 1;
            }, weapon => weapon.stage == 0),
            (0.3f, (item, weapon, dryRun) => { // lifesteal
                weapon.PostHit += (Player player, NPC npc, NPC.HitInfo hitInfo, int damageDone) => {
                    int lifeStealAmount = (int)(damageDone * 0.02f); // 2% lifesteal
                    player.Heal(lifeStealAmount);
                };
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 0.85f);
                item.knockBack += 1;
            }, weapon => weapon.stage == 0),
            (0.4f, (item, weapon, dryRun) => { // poison dot
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    npc.AddBuff(BuffID.Poisoned, 180);
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 0.9f);
            }, weapon => weapon.stage == 0),
            (0.4f, (item, weapon, dryRun) => {
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    npc.AddBuff(BuffID.OnFire, 240); // fire for 4 seconds
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 0.9f);
                item.useTime += 3;
            }, weapon => weapon.stage == 0),
            (0.3f, (item, weapon, dryRun) => {
                weapon.OnShoot += (Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) => {

                };
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * (stage == 0 ? 0.6f : 0.9f));
            }, weapon => weapon.stage == 0 && weapon.type >= WeaponType.Tome && weapon.type <= WeaponType.Thrown && weapon.type != WeaponType.Whip),
            (0.3f, (item, weapon, dryRun) => {
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    npc.AddBuff(BuffID.Bleeding, 180);
                if (dryRun)
                    return;
                item.crit += 5;
                item.defense -= 3; // reduce holder defense (needs impl)
            }, weapon => weapon.stage == 0),
            (0.05f, (item, weapon, dryRun) => { // shoot scream thing
                if (dryRun)
                    return;
                weapon.weaponStats[0] |= 0b01000000;
                item.knockBack += 2;
            }, weapon => true),
            (0.3f, (item, weapon, dryRun) => { // lower enemy defense
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    modifiers.Defense.Base -= 3;
                if (dryRun)
                    return;
                item.useTime += 2;
            }, weapon => weapon.stage == 0 && weapon.type >= WeaponType.Tome && weapon.type <= WeaponType.Thrown && weapon.type != WeaponType.Whip),
            (0.13f, (item, weapon, dryRun) => {
                weapon.OnHit += (Player player, NPC npc, ref NPC.HitModifiers modifiers) =>
                    npc.AddBuff(BuffID.Slow, 120);
                if (dryRun)
                    return;
                item.crit -= 2;
                item.useTime += 12;
                item.damage = (int)(item.damage * 1.1f);
            }, weapon => true),
            (0.1f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.useTime = item.useTime * 2 + 13;
                item.damage = (int)(item.damage * 1.6f);
                item.knockBack *= 2;
            }, weapon => stage == 0),
            (0.2f, (item, weapon, dryRun) => { // guardian spirit thingy
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 0.85f);
            }, weapon => weapon.stage == 0),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.knockBack *= 1.2f;
            }, weapon => weapon.stage == 0),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.shootSpeed *= 1.15f;
            }, weapon => weapon.stage == 0 && weapon.type >= WeaponType.Yoyo && weapon.type <= WeaponType.Thrown),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.damage = (int)(item.damage * 1.2f);
            }, weapon => weapon.stage == 0),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.crit += 3;
            }, weapon => true),
            (0.2f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.pick = (int)(item.pick * 1.2f);
            }, weapon => weapon.type == WeaponType.Pickaxe),
            (0.1f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.knockBack *= 0.6f;
            }, weapon => weapon.stage == 0),
            (0.1f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.shootSpeed *= 0.7f;
            }, weapon => weapon.stage == 0 && weapon.type >= WeaponType.Yoyo && weapon.type <= WeaponType.Thrown),
            (0.1f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.crit -= 1;
            }, weapon => weapon.stage == 0),
            (0.1f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.pick = (int)(item.pick * 0.7f);
            }, weapon => weapon.type == WeaponType.Pickaxe),
            (0.0001f, (item, weapon, dryRun) => {
                if (dryRun)
                    return;
                item.pick *= 100;
            }, weapon => weapon.type == WeaponType.Pickaxe),
        ];
    }

    public override void Unload() {
        meleeFrames = shinyMeleeFrames = yoyoFrames = tomeFrames = scepterFrames = staffFrames =
            whipFrames = pistolFrames = assaultRifleFrames = rifleFrames = bowFrames =
            thrownFrames = pickaxeFrames = handleFrames = tomePatternFrames = staffGemFrames =
            pistolHandleFrames = assaultRifleHandleFrames = rifleHandleFrames = miscFrames = null;
        materials = null;
        modifiers = null;
    }

    public override void SetDefaults() {
        type = WeaponType.Melee;
        //Item.useStyle = ItemUseStyleID.Swing;
        frame = [new(), new(), new()];
        weaponStats = new byte[1];
        materialIDs = [0, 0, 0];
        stage = 10;
        dust = 0;
        name = "???";
        Item.width = 40;
        Item.height = 40;
    }

    public override void OnCreated(ItemCreationContext context) {
        SoulWeaponID = null;
        type = (WeaponType)Main.rand.Next((int)WeaponType.Count);
        int gunType = Main.rand.Next(3);
        if (type == WeaponType.RangedMelee || type == WeaponType.Gun)
            subType = (SubType)(gunType + (type == WeaponType.Gun ? 3 : 0));
        Frame[] a = GetPrimaryFrameArray();
        Frame[] b = GetSecondaryFrameArray();
        frame = [
            a.Length > 0 ? Main.rand.NextFromList(a) : new Frame(),
            b.Length > 0 ? Main.rand.NextFromList(b) : new Frame(),
            Main.rand.NextFromList(miscFrames)
        ];
        materialIDs = [(byte)Main.rand.Next(materials.Length), (byte)Main.rand.Next(materials.Length), (byte)Main.rand.Next(materials.Length)];
        modifierIDs = GetModifiers();
        //float damageCalc = stage == 0 ? 1 : Item.damage / stage switch { 1 => 33f, 2 => 39f, 3 => 45f, 4 => 61f, 5 => 71f, 6 => 83f, 7 => 105f, 8 => 151f, 9 => 401f, _ => 401f };
        int actualStage = 1; // stage calcs
        Item.damage = actualStage switch {
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
        weaponStats = new byte[type switch {
            WeaponType.Yoyo or WeaponType.Tome or WeaponType.Scepter or WeaponType.Staff or WeaponType.Gun or WeaponType.Bow or WeaponType.Thrown => 4,
            _ => 1
        }];
        for (byte i = 0; i <= actualStage; i++) {
            stage = i;
            foreach (byte modifier in modifierIDs)
                if (modifiers[modifier].apply(this))
                    modifiers[modifier].modifier(Item, this, false);
        }
        name = GenerateName();
        Init();
    }

    public void Init() {
        Item.useTime = 20;
        Item.useAnimation = 20;
        Item.UseSound = SoundID.Item1;
        Item.knockBack = 6f;
        switch (type) {
            case WeaponType.Melee:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Swing;
                break;
            case WeaponType.RangedMelee:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.useAnimation = 20;
                Item.useTime = 20;
                if (subType == SubType.Projectile) {
                    Item.shoot = ProjectileID.PurificationPowder;
                    Item.shootSpeed = 10;
                } else if (subType >= SubType.FancySlash) {
                    Item.shoot = ModContent.ProjectileType<EnergySlash>();
                    Item.noMelee = true;
                    Item.shootsEveryUse = true;
                    Item.knockBack = 4.5f;
                    Item.shootSpeed = 10;
                }
                break;
            case WeaponType.Yoyo:
                Item.DamageType = DamageClass.MeleeNoSpeed;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<Yoyo>();
                Item.shootSpeed = 16f;
                Item.useTime = 25;
                Item.useAnimation = 25;
                Item.channel = true;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                break;
            case WeaponType.Tome:
                Item.UseSound = SoundID.Item13;
                Item.DamageType = DamageClass.Magic;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ProjectileID.PurificationPowder;
                Item.shootSpeed = 6f;
                Item.mana = 10;
                break;
            case WeaponType.Scepter:
                Item.UseSound = SoundID.Item13;
                Item.DamageType = DamageClass.Magic;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ProjectileID.PurificationPowder;
                Item.shootSpeed = 6f;
                Item.mana = 10;
                break;
            case WeaponType.Staff:
                Item.UseSound = SoundID.Item13;
                Item.DamageType = DamageClass.Magic;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ProjectileID.PurificationPowder;
                Item.shootSpeed = 6f;
                Item.mana = 10;
                break;
            case WeaponType.Whip:
                Item.DamageType = DamageClass.SummonMeleeSpeed;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.shoot = ModContent.ProjectileType<Whip>();
                Item.shootSpeed = 2f;
                Item.useTime = 30;
                Item.useAnimation = 30;
                //Item.channel = true;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                break;
            case WeaponType.Gun:
                Item.UseSound = SoundID.Item41;
                Item.DamageType = DamageClass.Ranged;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ProjectileID.PurificationPowder;
                Item.shootSpeed = 14f;
                Item.useAmmo = AmmoID.Bullet;
                break;
            case WeaponType.Bow:
                Item.UseSound = SoundID.Item5;
                Item.DamageType = DamageClass.Ranged;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ProjectileID.PurificationPowder;
                Item.shootSpeed = 6f;
                Item.useAmmo = AmmoID.Arrow;
                break;
            case WeaponType.Thrown:
                Item.DamageType = DamageClass.Ranged;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.shoot = ModContent.ProjectileType<Thrown>();
                Item.useTime = 7;
                Item.useAnimation = 7;
                Item.shootSpeed = 6f;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                break;
            case WeaponType.Pickaxe:
                Item.DamageType = DamageClass.Melee;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.autoReuse = true;
                Item.pick = (int)Math.Round((500 * (1 / (1 + Math.Pow(Math.E, -0.3 * (stage - 5)))) - 60) / 10) * 10;
                break;
            default:
                Item.DamageType = DamageClass.Default;
                Item.useStyle = ItemUseStyleID.Swing;
                Item.damage = 1;
                break;
        }
        Item.width = 0;
        Item.height = 0;
        for (int i = 0; i < frame.Length; i++) {
            if (frame[i].texture != null) {
                Item.width += frame[i].x;
                Item.height += frame[i].y;
            }
        }
        Item.value = Item.sellPrice(gold: (int)Math.Pow(stage + 1, 1.7), silver: (int)Math.IEEERemainder(Math.Pow(stage + 1, 1.7) * 100, 100));
        Item.rare = stage + 1;
        Item.SetNameOverride(name);
        Item.NetStateChanged();
    }

    public byte[] GetModifiers(int stage = 0) {
        int modifierCount = Main.rand.Next(2, Math.Min((int)Math.Pow(stage + 1, 0.6), 5) + 1);
        byte[] weaponModifiers = new byte[modifierCount];

        for (int i = 0; i < modifierCount; i++) {
            // randomly pick a modifier based on weight
            var possibleModifiers = modifiers.Where(m => m.apply(this) && !weaponModifiers.Contains((byte)Array.IndexOf(modifiers, m))).ToArray();
            if (possibleModifiers.Length == 0)
                break;

            float totalWeight = possibleModifiers.Sum(m => m.weight);
            float roll = (float)Main.rand.NextDouble() * totalWeight;
            float currentWeight = 0;

            for (byte j = 0; j < possibleModifiers.Length; j++) {
                var modifier = possibleModifiers[j];
                currentWeight += modifier.weight;
                if (roll <= currentWeight) {
                    weaponModifiers[i] = j;
                    break;
                }
            }
        }
        return weaponModifiers;
    }

    public Texture2D MergeTextures(Texture2D texture1, Texture2D texture2, Vector2 offset) {
        int newWidth = Item.width;
        int newHeight = Item.height;

        Texture2D mergedTexture = new(Main.graphics.GraphicsDevice, newWidth, newHeight);

        Color[] texture1Data = new Color[texture1.Width * texture1.Height];
        Color[] texture2Data = new Color[texture2.Width * texture2.Height];
        Color[] mergedData = new Color[newWidth * newHeight];
        texture1.GetData(texture1Data);
        texture2.GetData(texture2Data);

        for (int y = 0; y < texture1.Height; y++) {
            for (int x = 0; x < texture1.Width; x++) {
                int mergedIndex = (y + (newHeight - texture1.Height)) * newWidth + x;
                mergedData[mergedIndex] = texture1Data[y * texture1.Width + x];
            }
        }

        for (int y = 0; y < texture2.Height; y++) {
            for (int x = 0; x < texture2.Width; x++) {
                int mergedX = x + (int)offset.X;
                int mergedY = y + (newHeight - texture2.Height - (int)offset.Y);

                if (mergedX >= 0 && mergedX < newWidth && mergedY >= 0 && mergedY < newHeight) {
                    int mergedIndex = mergedY * newWidth + mergedX;
                    Color colorToSet = texture2Data[y * texture2.Width + x];

                    if (colorToSet.A > 0)
                        mergedData[mergedIndex] = colorToSet;
                }
            }
        }

        mergedTexture.SetData(mergedData);
        return mergedTexture;
    }

    public void ConstructTexture() {
        Texture2D t;
        switch (type) {
            case WeaponType.Gun:
            case WeaponType.Pickaxe:
            case WeaponType.RangedMelee:
            case WeaponType.Melee:
                if (frame[1].texture != null)
                    t = MergeTextures(frame[1].texture.Value, frame[0].texture.Value, new(frame[1].x + frame[0].offsetX, frame[1].y + frame[0].offsetY));
                else if (frame[0].texture != null)
                    t = frame[0].texture.Value;
                else
                    t = ModContent.Request<Texture2D>(Texture).Value;
                break;
            default:
                if (frame[1].texture != null)
                    t = MergeTextures(frame[0].texture.Value, frame[1].texture.Value, new(frame[0].x + frame[1].offsetX, frame[0].y + frame[1].offsetY));
                else if (frame[0].texture != null)
                    t = frame[0].texture.Value;
                else
                    t = ModContent.Request<Texture2D>(Texture).Value;
                break;
        }
        texture = t;
    }

    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle f, Color drawColor, Color itemColor, Vector2 origin, float scale) {
        if (texture == null)
            ConstructTexture();
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);

        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"]
                .UseImage0(materials[materialIDs[0]].material)
                .UseImage1(materials[materialIDs[1]].material)
                .UseImage2(materials[materialIDs[2]].material)
                .Apply();
        spriteBatch.Draw(texture, position, new Rectangle(0, 0, Item.width, Item.height), drawColor, 0, origin, scale, SpriteEffects.None, 0);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        
        return false;
    }

    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI) {
        if (texture == null)
            ConstructTexture();
        Vector2 drawOrigin = new(Item.width / 2f, Item.height / 2f);
        Vector2 drawPosition = Item.Bottom - Main.screenPosition - new Vector2(0, drawOrigin.Y);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, null, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"]
                .UseImage0(materials[materialIDs[0]].material)
                .UseImage1(materials[materialIDs[1]].material)
                .UseImage2(materials[materialIDs[2]].material)
                .Apply();
        spriteBatch.Draw(texture, drawPosition, new Rectangle(0, 0, Item.width, Item.height), lightColor, rotation, drawOrigin, scale, SpriteEffects.None, 0);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }

    public Frame[] GetPrimaryFrameArray() {
        return type switch {
            WeaponType.Melee => meleeFrames,
            WeaponType.RangedMelee => shinyMeleeFrames,
            WeaponType.Yoyo => yoyoFrames,
            WeaponType.Tome => tomeFrames,
            WeaponType.Scepter => scepterFrames,
            WeaponType.Staff => staffFrames,
            WeaponType.Whip => whipFrames,
            WeaponType.Gun => subType == SubType.Pistol ? pistolFrames :
                                  subType == SubType.AssaultRifle ? assaultRifleFrames :
                                  subType == SubType.Rifle ? rifleFrames : [],
            WeaponType.Bow => bowFrames,
            WeaponType.Thrown => thrownFrames,
            WeaponType.Pickaxe => pickaxeFrames,
            _ => []
        };
    }

    public Frame[] GetSecondaryFrameArray() {
        return type switch {
            WeaponType.Melee or WeaponType.RangedMelee => handleFrames,
            WeaponType.Tome => tomePatternFrames,
            WeaponType.Staff => staffGemFrames,
            WeaponType.Gun => subType == SubType.Pistol ? pistolHandleFrames :
                                  subType == SubType.AssaultRifle ? assaultRifleHandleFrames :
                                  subType == SubType.Rifle ? rifleHandleFrames : [],
            WeaponType.Pickaxe => pickaxeHandleFrames,
            _ => []
        };
    }

    public override void NetSend(BinaryWriter writer) {
        writer.Write(SoulWeaponID.ToByteArray());
        writer.Write((byte)type);
        if (type == WeaponType.RangedMelee || type == WeaponType.Gun)
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
        writer.Write((byte)modifierIDs.Length);
        writer.Write(modifierIDs);
        writer.Write(Item.damage);
    }

    public override void NetReceive(BinaryReader reader) {
        SoulWeaponID = new(reader.ReadBytes(16));
        type = (WeaponType)reader.ReadByte();
        if (type == WeaponType.RangedMelee || type == WeaponType.Gun)
            subType = (SubType)reader.ReadByte();
        byte[] frames = reader.ReadBytes(3);
        frame = [frames[0] > 0 ? GetPrimaryFrameArray()[frames[0] - 1] : new Frame(),
            frames[1] > 0 ? GetSecondaryFrameArray()[frames[1] - 1] : new Frame(),
            frames[2] > 0 ? miscFrames[frames[2] - 1] : new Frame()];
        materialIDs = reader.ReadBytes(3);
        stage = reader.ReadByte();
        name = reader.ReadString();
        bool dryRun = modifierIDs.Length != 0;
        modifierIDs = reader.ReadBytes(reader.ReadByte());
        foreach (byte modifier in modifierIDs)
            modifiers[modifier].modifier(Item, this, dryRun);
        Item.damage = reader.ReadInt32();
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
        Item.rare = ItemRarityID.White;
        Item.scale = 1f;
        Item.shoot = ProjectileID.None;
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
        if (type == WeaponType.RangedMelee && subType != SubType.Projectile || type == WeaponType.Gun && subType != SubType.Pistol)
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
        tag["modifiers"] = modifierIDs;
        tag["damage"] = Item.damage;
        switch (type) {
            case WeaponType.Melee:
                break;
            case WeaponType.RangedMelee:
                break;
            case WeaponType.Yoyo:
                break;
            case WeaponType.Tome:
                break;
            case WeaponType.Scepter:
                break;
            case WeaponType.Staff:
                break;
            case WeaponType.Whip:
                break;
            case WeaponType.Gun:
                break;
            case WeaponType.Bow:
                break;
            case WeaponType.Thrown:
                break;
            case WeaponType.Pickaxe:
                tag["pick"] = Item.pick;
                break;
        }
    }

    public override void LoadData(TagCompound tag) {
        Reset();
        if (tag.TryGet("id", out byte[] id))
            SoulWeaponID = new UUID(id);
        if (tag.TryGet("type", out byte t))
            type = (WeaponType)t;
        if (type == WeaponType.RangedMelee || type == WeaponType.Gun)
            if (tag.TryGet("subType", out byte st))
                subType = (SubType)st;
            else if (type == WeaponType.RangedMelee)
                subType = SubType.Projectile;
            else if (type == WeaponType.Gun)
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
        if (tag.TryGet("modifiers", out byte[] mo)) {
            modifierIDs = mo;
            foreach (byte modifier in modifierIDs)
                modifiers[modifier].modifier(Item, this, true);
        }
        Item.damage = tag.TryGet("damage", out int dmg) ? dmg : 1;
        switch (type) {
            case WeaponType.Melee:
                break;
            case WeaponType.RangedMelee:
                break;
            case WeaponType.Yoyo:
                break;
            case WeaponType.Tome:
                break;
            case WeaponType.Scepter:
                break;
            case WeaponType.Staff:
                break;
            case WeaponType.Whip:
                break;
            case WeaponType.Gun:
                break;
            case WeaponType.Bow:
                break;
            case WeaponType.Thrown:
                break;
            case WeaponType.Pickaxe:
                Item.pick = tag.TryGet("pick", out int p) ? p : 10;
                break;
        }
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
            player.DropItem(player.GetSource_FromThis("IncompatibleSoulWeapon"), player.position, ref Unsafe.AsRef(Item));
    }

    public override bool CanRightClick() => SoulWeaponID is null && Main.LocalPlayer.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID is null && !Main.LocalPlayer.inventory.Where(i => i.ModItem is SoulWeapon s && s != this && i.favorited).Any(); // prevent player from binding to a soul weapon if they have another soul weapon favorited

    public override void RightClick(Player player) {
        SoulWieldingPlayer s = player.GetModPlayer<SoulWieldingPlayer>();
        if (s.SoulWeaponID is null && SoulWeaponID is null)
            s.SoulWeaponID = SoulWeaponID = new();
    }

    public override bool ConsumeItem(Player player) => false; // prevents right clicking from deleting the soul weapon

    public override void ModifyHitNPC(Player player, NPC target, ref NPC.HitModifiers modifiers) => OnHit?.Invoke(player, target, ref modifiers);

    public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => PostHit?.Invoke(player, target, hit, damageDone);

    public override void ModifyWeaponDamage(Player player, ref StatModifier damage) {
        UUID uuid = player.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID;
        if (uuid is null && SoulWeaponID is null || uuid is not null && SoulWeaponID is null || SoulWeaponID is null && uuid is not null) // weapon is unbound/somehow the player has the item in their inventory despite it belonging to another player
            damage /= 2; // give a harsh penalty to the weapon's damage if its unbound, to discourage using unbound weapons
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int pType, int damage, float knockback) {
        if (type == WeaponType.RangedMelee && subType >= SubType.FancySlash) {
            float adjScale = player.GetAdjustedItemScale(Item);
            Projectile.NewProjectile(source, player.MountedCenter, new Vector2(player.direction, 0), pType, damage, knockback, player.whoAmI, player.direction * player.gravDir, player.itemAnimationMax, adjScale);
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);
        }
        if (type == WeaponType.RangedMelee && subType != SubType.FancySlash) {
            Projectile.NewProjectile(source, player.MountedCenter, velocity, ModContent.ProjectileType<EnergySword>(), damage, knockback, player.whoAmI);
            //EnergySword p = Main.projectile[proj].ModProjectile as EnergySword;
            //p.texture = texture;
            //p.Init();
        }

        return type != WeaponType.RangedMelee || subType != SubType.Projectile;
    }

    public override bool CanPickup(Player player) => CanPlayerWield(player);

    public override bool CanStack(Item source) => false;

    public bool CanPlayerWield(Player player) {
        UUID uuid = player.GetModPlayer<SoulWieldingPlayer>().SoulWeaponID;
        return uuid is null && SoulWeaponID is null || uuid is not null && SoulWeaponID is not null && uuid == SoulWeaponID;
    }

    public override void AddRecipes() {
        /*CreateRecipe()
            .AddIngredient(ItemID.DirtBlock, 10)
            .AddTile(TileID.WorkBenches)
            .Register();*/
    }
}
