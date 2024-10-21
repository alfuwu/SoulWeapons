using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SoulWeapons.Content.Items;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace SoulWeapons.Content.Projectiles;

public class Thrown : ModProjectile {
    public Texture2D texture;
    byte material1, material2, material3;
    Func<Color> color;

    public override void SetDefaults() {
        Projectile.friendly = true;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 2;
        Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.aiStyle = ProjAIStyleID.ThrownProjectile;
    }

    public override void OnSpawn(IEntitySource source) {
        if (source is EntitySource_ItemUse item && item.Item.ModItem is SoulWeapon s && s.texture != null && s.materialIDs.Length >= 1) {
            texture = s.texture;
            color = SoulWeapon.materials[s.materialIDs[0]].color;
            Projectile.scale = s.Item.scale;
            material1 = s.materialIDs[0];
            if (s.materialIDs.Length >= 3) {
                material2 = s.materialIDs[1];
                material3 = s.materialIDs[2];
            } else if (s.materialIDs.Length >= 2) {
                material2 = s.materialIDs[1];
            }
        }
    }

    public override bool PreDraw(ref Color lightColor) {
        Vector2 origin = new(texture.Width / 2, texture.Height / 2);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"].Shader.Parameters["material1"].SetValue(SoulWeapon.materials[material1].material.Value);
        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"].Shader.Parameters["material2"].SetValue(SoulWeapon.materials[material2].material.Value);
        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"].Shader.Parameters["material3"].SetValue(SoulWeapon.materials[material3].material.Value);
        GameShaders.Misc[$"{nameof(SoulWeapons)}/Weapon"].Apply();
        
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, default, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        return false;
    }
}
