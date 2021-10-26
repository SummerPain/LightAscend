using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using LightAscend.Sounds.Custom;

namespace LightAscend.Projectiles
{
	class ShiningStar : ModProjectile
	{
        private float LaserCheck
        {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        private const float MaxSize = 1.4f;
		private const float MinSize = 0f;
		private const float ShiningStarLifeTime = 60*3f;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Shining Star");
		}
		public override void SetDefaults()
		{
			projectile.timeLeft = (int)ShiningStarLifeTime;
			projectile.magic = true;
			projectile.width = 72;
			projectile.height = 72;

			projectile.penetrate = -1;
			projectile.friendly = true;
			projectile.ranged = false;
			projectile.netImportant = true;
			projectile.light = 0.7f;
			projectile.knockBack = 4f;
			projectile.tileCollide = false;
			projectile.alpha = 40;

			//drawOffsetX = -18;
			//drawOriginOffsetY = -17;
		}
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 5;
		}
		public override void AI()
		{
            #region onStart
            if (projectile.timeLeft >= ShiningStarLifeTime)
            {
				if (!Main.dedServ)
				{
					Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/ShiningStarSound").WithVolume(6.6f).WithPitchVariance(.4f), projectile.Center);
				}
			}
            #endregion

            #region typeCheck
            if (projectile.type != ModContent.ProjectileType<ShiningStar>())
			{
				projectile.Kill();
				return;
			}
            #endregion

            #region visuals
            //rotation
            projectile.rotation += 0.016f;

			//spiral-ish dust

			Vector2 speed = new Vector2(1f, 0f);
			speed = speed.RotatedBy(projectile.timeLeft/9f);

			Dust sd = Dust.NewDustPerfect(projectile.Center, DustID.AncientLight, speed*8, Scale: 1.5f);
			//Dust dust2 = Main.dust[Dust.NewDust(dustCentre, 0, 0, DustID.AncientLight, dustVel.X, dustVel.Y)];
			sd.noGravity = true;
			sd.color = Color.White;

			Dust isd = Dust.NewDustPerfect(projectile.Center, DustID.AncientLight, -speed*8, Scale: 1.5f);
			//Dust dust2 = Main.dust[Dust.NewDust(dustCentre, 0, 0, DustID.AncientLight, dustVel.X, dustVel.Y)];
			isd.noGravity = true;
			isd.color = Color.White;
			#endregion

			#region laserShoot
			//when scaled at max (1/2 of life) launch laser
			if (ScaleStar() && LaserCheck == 0)
            {
				int uuid = Projectile.GetByUUID(projectile.owner, projectile.whoAmI);

				int damage = projectile.damage;
				float knockback = projectile.knockBack;

				Vector2 beamDir = new Vector2(1,0);

				Projectile.NewProjectile(projectile.Center, beamDir, ModContent.ProjectileType<ShiningLaser>(), damage, knockback, projectile.owner, 0, uuid);
				LaserCheck = 1;
			}
            #endregion

            #region onDeath
            //when dying
            if (projectile.timeLeft <= 1)
			{
				float velMult = 50f;
				Vector2 dustCentre = projectile.Center;
				for (int i = 0; i < 20; i++)
				{
					Vector2 dustVel = Main.rand.NextVector2CircularEdge(1f, 1f) * velMult;
					Dust d = Dust.NewDustPerfect(dustCentre + dustVel, DustID.AncientLight, dustVel/2, Scale: 2.4f);
					//Dust dust2 = Main.dust[Dust.NewDust(dustCentre, 0, 0, DustID.AncientLight, dustVel.X, dustVel.Y)];
					d.noGravity = true;
					d.color = Color.White;
				}
				Main.PlaySound(SoundID.Item, projectile.Center, 14);
			}
            #endregion
        }
        public override void Kill(int timeLeft)
		{
			// This code and the similar code above in OnTileCollide spawn dust from the tiles collided with. SoundID.Item10 is the bounce sound you hear.
			//Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
			//shine?
			
		}
		private bool ScaleStar()
        {
			//it means for half of lifetime scaling to the MaxSize
			float scalingRatio = MathHelper.Clamp((ShiningStarLifeTime - projectile.timeLeft) / (ShiningStarLifeTime / 2), 0f, 1f);
			projectile.scale = MathHelper.Lerp(MinSize, MaxSize, scalingRatio);
			return scalingRatio >= 1f;
		}
	}
}
