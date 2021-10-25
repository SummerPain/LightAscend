using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LightAscend.Projectiles
{
    class SheeshProjectile : ModProjectile
    {
		private const float MOVE_DISTANCE = 5f;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("xdxdxd");
			ProjectileID.Sets.TrailCacheLength[projectile.type] = 10;    //The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[projectile.type] = 1;        //The recording mode
		}
		
		public override void SetDefaults()
		{
			projectile.timeLeft = 100;
			projectile.magic = true;
			projectile.width = 40;
			projectile.height = 8;
			
			projectile.penetrate = 3;
			projectile.friendly = true;
			projectile.ranged = false;
			projectile.magic = true;
			projectile.netImportant = true;
			projectile.light = 0.5f;
			projectile.knockBack = 10f;
			projectile.extraUpdates = 1;
			projectile.alpha = 100;
			//drawOriginOffsetX = -15;
			drawOriginOffsetY = -10;
			//drawOffsetX = -26;
			drawOffsetX = -20;
		}
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 5;
		}
		public override void AI()
        {
			//on spawn
			if (projectile.ai[0] <= 0)
            {
				if (projectile.owner == Main.myPlayer)
                {
					projectile.Center = Main.player[projectile.owner].Center + projectile.velocity * MOVE_DISTANCE;
					// shot particles
					for (int i = 0; i < 10; i++)
					{
						Vector2 speed = Main.rand.NextVector2CircularEdge(.1f, .1f);
						Dust d = Dust.NewDustPerfect(Main.player[projectile.owner].Center + projectile.velocity * MOVE_DISTANCE*3.5f, DustID.Clentaminator_Blue, speed * 10, Scale: 1.5f);
						d.noGravity = true;
					}
				}
				projectile.velocity *= 10f/projectile.extraUpdates;
			} //straight fly
			else
            {
				projectile.velocity = projectile.oldVelocity;
			}
			//rotation
			projectile.rotation = projectile.velocity.ToRotation();

			//timer
			projectile.ai[0]++;

			//trail
			if (projectile.ai[0] >= 7)
            {
				Dust dust = Dust.NewDustDirect(projectile.Center, projectile.width, projectile.height, DustID.Clentaminator_Cyan,
				-projectile.velocity.X/2, -projectile.velocity.Y/2, Alpha: 60, default(Color), Scale: 0.7f);
				projectile.ai[0] = 2;
			}

			if (projectile.penetrate <= 1)
            {
				Explode();
                //if (projectile.ai[1] >= 3)
                //{
                //    projectile.Kill();
                //}
                //else
                //{
                //    projectile.ai[1]++;
                //}
            }
			//when explosion is triggered life is draining
			if(projectile.ai[1] >= 1)
            {
				projectile.ai[1]++;
				
			}
			if (projectile.ai[1] >= 3)
            {
				projectile.Kill();
            }
		}
		public void Explode()
        {
			if (projectile.owner == Main.myPlayer && projectile.ai[1] <= 0)
			{
				projectile.tileCollide = false;
				projectile.velocity = new Vector2(0, 0);
				// Set to transparent. This projectile technically lives as  transparent for about 3 frames
				projectile.alpha = 255;
				projectile.penetrate = 2000;
				// change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
				projectile.position = projectile.Center;
				//projectile.damage = projectile.damage/(4/3);
				projectile.knockBack = 10f;
				projectile.width = 250;
				projectile.height = 250;
				projectile.Center = projectile.position;
				projectile.ai[1] = 1;
				//explosion particles
				for (int i = 0; i < 40; i++)
				{
					Vector2 speed = Main.rand.NextVector2CircularEdge(1.1f, 1.1f);
					Dust d = Dust.NewDustPerfect(projectile.Center, DustID.SpectreStaff, speed * 10, Scale: 2.5f);
					d.noGravity = true;
				}
			}
		}
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
			Explode();
			return false;
        }
        public override void Kill(int timeLeft)
		{
			// This code and the similar code above in OnTileCollide spawn dust from the tiles collided with. SoundID.Item10 is the bounce sound you hear.
			//Collision.HitTiles(projectile.position + projectile.velocity, projectile.velocity, projectile.width, projectile.height);
			Main.PlaySound(SoundID.Item10, projectile.position);
			for (int i = 0; i < 7; i++)
			{
				Vector2 speed = Main.rand.NextVector2CircularEdge(.1f, .1f);
				Dust d = Dust.NewDustPerfect(projectile.Center, DustID.Clentaminator_Blue, speed * 50, Scale: 1f);
				d.noGravity = true;
			}
		}
	}
}
