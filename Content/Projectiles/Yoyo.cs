﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulWeapons.Content.Projectiles;

public class Yoyo : ModProjectile
{
    public float HoverX
    {
        get => Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public float HoverY
    {
        get => Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public bool Retracting
    {
        get => Projectile.ai[0] < 0;
        set => Projectile.ai[0] = -1f;
    }

    public float Age
    {
        get => Projectile.localAI[0];
        set => Projectile.localAI[0] = value;
    }

    public override void SetStaticDefaults()
    {
        // The following sets are only applicable to yoyo that use aiStyle 99.

        // YoyosLifeTimeMultiplier is how long in seconds the yoyo will stay out before automatically returning to the player. 
        // Vanilla values range from 3f (Wood) to 16f (Chik), and defaults to -1f. Leaving as -1 will make the time infinite.
        ProjectileID.Sets.YoyosLifeTimeMultiplier[Projectile.type] = 3.5f;

        // YoyosMaximumRange is the maximum distance the yoyo sleep away from the player. 
        // Vanilla values range from 130f (Wood) to 400f (Terrarian), and defaults to 200f.
        ProjectileID.Sets.YoyosMaximumRange[Projectile.type] = 300f;

        // YoyosTopSpeed is top speed of the yoyo Projectile.
        // Vanilla values range from 9f (Wood) to 17.5f (Terrarian), and defaults to 10f.
        ProjectileID.Sets.YoyosTopSpeed[Projectile.type] = 13f;
    }

    public override void SetDefaults()
    {
        Projectile.width = 16; // The width of the projectile's hitbox.
        Projectile.height = 16; // The height of the projectile's hitbox.

        Projectile.aiStyle = ProjAIStyleID.Yoyo; // The projectile's ai style. Yoyos use aiStyle 99 (ProjAIStyleID.Yoyo). A lot of yoyo code checks for this aiStyle to work properly.

        Projectile.friendly = true; // Player shot projectile. Does damage to enemies but not to friendly Town NPCs.
        Projectile.DamageType = DamageClass.MeleeNoSpeed; // Benefits from melee bonuses. MeleeNoSpeed means the item will not scale with attack speed.
        Projectile.penetrate = -1; // All vanilla yoyos have infinite penetration. The number of enemies the yoyo can hit before being pulled back in is based on YoyosLifeTimeMultiplier.
                                   // Projectile.scale = 1f; // The scale of the projectile. Most yoyos are 1f, but a few are larger. The Kraken is the largest at 1.2f
    }
}
