using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria;
using Terraria.ModLoader;
using Terraria.Graphics.Shaders;
using SoulWeapons.Content.Items;

namespace SoulWeapons.Content.Projectiles;

public class EnergySword : ModProjectile {
    public Texture2D texture;

    public override void SetDefaults() {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 2;
        Projectile.tileCollide = true;// false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.aiStyle = ProjAIStyleID.Beam;
        AIType = ProjectileID.LightBeam;
    }

    public void Init() {
        //Projectile.width = texture.Width;
        //Projectile.height = texture.Height;
    }

    public override void OnSpawn(IEntitySource source) {
        if (source is EntitySource_ItemUse itemUse && itemUse.Item.ModItem is SoulWeapon s && s.texture != null) {
            texture = s.texture;
            Init();
        }
    }

    public override void SendExtraAI(BinaryWriter writer) {

    }

    public override void ReceiveExtraAI(BinaryReader reader) {

    }

    public override void AI() {
        
    }

    public override void EmitEnchantmentVisualsAt(Vector2 boxPosition, int boxWidth, int boxHeight) {
        
    }

    public override bool PreDraw(ref Color lightColor) {
        Vector2 origin = new(texture.Width / 2, texture.Height / 2);
        //Main.spriteBatch.End();
        //Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

        //GameShaders.Misc[$"{nameof(SoulWeapons)}/EnergySword"].UseShaderSpecificData(new Vector4(texture.Width / 2, texture.Height / 2, 0, 0));
        //GameShaders.Misc[$"{nameof(SoulWeapons)}/EnergySword"].UseColor(Color.Gold);
        //GameShaders.Misc[$"{nameof(SoulWeapons)}/EnergySword"].Apply();

        bool flip = Math.Cos(Projectile.rotation - MathHelper.PiOver4) > 0;
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, default, Color.White, flip ? Projectile.rotation : Projectile.rotation + MathHelper.PiOver2, origin, Projectile.scale, flip ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
        //Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, default, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);

        //Main.spriteBatch.End();
        //Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        return false;
    }
}
