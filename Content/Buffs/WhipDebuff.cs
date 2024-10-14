using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulWeapons.Content.Buffs;

public class WhipDebuff : ModBuff {
    public const int TagDamagePercent = 30;
    public const float TagDamageMultiplier = TagDamagePercent / 100f;

    public override void SetStaticDefaults() => BuffID.Sets.IsATagBuff[Type] = true;
}

public class WhipDebuffFlat : ModBuff {
    public const int TagDamage = 5;

    public override void SetStaticDefaults() => BuffID.Sets.IsATagBuff[Type] = true;
}

public class WhipDebuffNPC : GlobalNPC {
    public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
        if (projectile.npcProj || projectile.trap || !projectile.IsMinionOrSentryRelated)
            return;

        var projTagMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
        if (npc.HasBuff<WhipDebuffFlat>())
            modifiers.FlatBonusDamage += WhipDebuffFlat.TagDamage * projTagMultiplier;

        if (npc.HasBuff<WhipDebuff>()) {
            modifiers.ScalingBonusDamage += WhipDebuff.TagDamageMultiplier * projTagMultiplier;
            npc.RequestBuffRemoval(ModContent.BuffType<WhipDebuff>());
        }
    }
}