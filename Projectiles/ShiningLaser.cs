using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ID;

namespace LightAscend.Projectiles
{
	// Manaless laser for ShiningStar
	public class ShiningLaser : ModProjectile
	{
		// distance from center(start)
		private const float MOVE_DISTANCE = 8f;

		// The actual distance is stored in the ai0 field
		// By making a property to handle this it makes our life easier, and the accessibility more readable
		public float Distance
		{
			get => projectile.ai[0];
			set => projectile.ai[0] = value;
		}
		private float HostStarIndex
		{
			get => projectile.ai[1];
			set => projectile.ai[1] = value;
		}
		public override void SetStaticDefaults()
		{
			// Signals to Terraria that this projectile requires a unique identifier beyond its index in the projectile array.
			// This prevents the issue with the vanilla Last Prism where the beams are invisible in multiplayer.
			ProjectileID.Sets.NeedsUUID[projectile.type] = true;
			ProjectileID.Sets.Homing[projectile.type] = true;
		}
		public override void SetDefaults()
		{
			projectile.width = 10;
			projectile.height = 10;
			projectile.friendly = true;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.magic = true;
			//projectile.hide = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			DrawLaser(spriteBatch, Main.projectileTexture[projectile.type], projectile.Center,
				projectile.velocity, 10, projectile.damage, -1.57f, 1f, 1000f, Color.White, (int)MOVE_DISTANCE);
			return false;
		}

		public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default(Color), int transDist = 50)
		{
			float r = unit.ToRotation() + rotation;

			// Draws the laser 'body'
			for (float i = transDist; i <= Distance; i += step)
			{
				Color c = Color.White;
				var origin = start + i * unit;
				spriteBatch.Draw(texture, origin - Main.screenPosition,
					new Rectangle(0, 26, 28, 26), i < transDist ? Color.Transparent : c, r,
					new Vector2(28 * .5f, 26 * .5f), scale, 0, 0);
			}

			// Draws the laser 'tail'
			spriteBatch.Draw(texture, start + unit * (transDist - step) - Main.screenPosition,
				new Rectangle(0, 0, 28, 26), Color.White, r, new Vector2(28 * .5f, 26 * .5f), scale, 0, 0);

			// Draws the laser 'head'
			spriteBatch.Draw(texture, start + (Distance + step) * unit - Main.screenPosition,
				new Rectangle(0, 52, 28, 26), Color.White, r, new Vector2(28 * .5f, 26 * .5f), scale, 0, 0);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 unit = projectile.velocity;
			float point = 0f;
			// Run an AABB versus Line check to look for collisions, look up AABB collision first to see how it works
			// It will look for collisions on the given line using AABB
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), projectile.Center,
				projectile.Center + unit * Distance, 22, ref point);
		}

		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 5;
			if (Main.rand.NextBool(180)) //the larger the number, the less chance
			{
				SpawnShinigStar(target.Center, (int)(projectile.damage/1.04f), 4f);
			}
		}
		private void SpawnShinigStar(Vector2 spawnPoint, int damage, float knockback)
        {
			int uuid = Projectile.GetByUUID(projectile.owner, projectile.whoAmI);
			Projectile.NewProjectileDirect(spawnPoint, new Vector2(0,0), ModContent.ProjectileType<ShiningStar>(), damage, knockback, projectile.owner, 0, uuid);
			projectile.netUpdate = true;
		}
		// The AI of the projectile/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		public override void AI()
		{
			Player player = Main.player[projectile.owner];

			Projectile hostStar = Main.projectile[(int)HostStarIndex];
			if (projectile.type != ModContent.ProjectileType<ShiningLaser>() || !hostStar.active || hostStar.type != ModContent.ProjectileType<ShiningStar>())
            {
                projectile.Kill();
                return;
            }
			projectile.Center = hostStar.Center + projectile.velocity * MOVE_DISTANCE;
			projectile.timeLeft = 2;

			#region Find target
			// Starting search distance
			float distanceFromTarget = 1600f;
			Vector2 targetCenter = projectile.position;
			bool foundTarget = false;

			// This code is required if your minion weapon has the targeting feature
			if (player.HasMinionAttackTargetNPC)
			{
				NPC npc = Main.npc[player.MinionAttackTargetNPC];
				float between = Vector2.Distance(npc.Center, projectile.Center);
				// Reasonable distance away so it doesn't target across multiple screens
				if (between < 2000f)
				{
					distanceFromTarget = between;
					targetCenter = npc.Center;
					foundTarget = true;
				}
			}
			if (!foundTarget)
			{
				// This code is required either way, used for finding a target
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC npc = Main.npc[i];
					if (npc.CanBeChasedBy())
					{
						float between = Vector2.Distance(npc.Center, projectile.Center);
						bool closest = Vector2.Distance(projectile.Center, targetCenter) > between;
						bool inRange = between < distanceFromTarget;
						bool lineOfSight = Collision.CanHitLine(projectile.position, projectile.width, projectile.height, npc.position, npc.width, npc.height);
						// Additional check for this specific minion behavior, otherwise it will stop attacking once it dashed through an enemy while flying though tiles afterwards
						// The number depends on various parameters seen in the movement code below. Test different ones out until it works alright
						bool closeThroughWall = between < 4f;
						if (((closest && inRange) || !foundTarget) && (lineOfSight || closeThroughWall))
						{
							distanceFromTarget = between;
							targetCenter = npc.Center;
							foundTarget = true;
						}
					}
				}
			}
			if (foundTarget)
            {
				projectile.velocity = Vector2.Normalize(targetCenter - hostStar.Center);
				Vector2 dustPos = targetCenter;
			}
			else
            {
				projectile.Kill();
            }
			// friendly needs to be set to true so the minion can deal contact damage
			// friendly needs to be set to false so it doesn't damage things like target dummies while idling
			// Both things depend on if it has a target or not, so it's just one assignment here
			// You don't need this assignment if your minion is shooting things instead of dealing contact damage
			projectile.friendly = foundTarget;
			#endregion

			UpdatePlayer(hostStar);

			SetLaserPosition();
			SpawnDusts(hostStar);
			CastLights();
		}
		private void SpawnDusts(Projectile host)
		{
			Vector2 dustPos = host.Center + host.velocity * Distance * 1.007f;

			float velMult = 1f;
            float num1 = projectile.velocity.ToRotation() + (Main.rand.Next(2) == 1 ? -1.0f : 1.0f) * 1.57f;
            float num2 = (float)(Main.rand.NextDouble() * 0.8f + 1.0f);
            Vector2 dustVel = new Vector2((float)Math.Cos(num1) * num2, (float)Math.Sin(num1) * num2) * velMult;
            Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.AncientLight, dustVel.X, dustVel.Y)];
            dust.noGravity = true;
            dust.scale = 2f;
			dust.color = Color.White;

			//shine?
			dustPos = projectile.Center;
			dustVel = Main.rand.NextVector2CircularEdge(1f, 1f) * velMult;
			for (int i = 0; i < 17; i++) {
				Dust d = Dust.NewDustPerfect(dustPos + dustVel * 50, DustID.AncientLight, dustVel, Scale: 2.4f);
				d.noGravity = true;
				d.color = Color.White;
			}
		}
		/*
		 * Sets the end of the laser position based on where it collides with something
		 */
		private void SetLaserPosition()
		{
			for (Distance = MOVE_DISTANCE; Distance <= 2200f; Distance += 5f)
			{
				var start = projectile.Center + projectile.velocity * Distance;
				if (!Collision.CanHitLine(projectile.Center, 1, 1, start, 1, 1) && !Collision.CanHit(projectile.Center, 1, 1, start, 1, 1))
				{
					Distance -= 5f;
					break;
				}
			}
		}
		
		private void UpdatePlayer(Projectile host)
		{
            // Multiplayer support here, only run this code if the client running it is the owner of the projectile
            if (projectile.owner == Main.myPlayer)
            {
                Vector2 diff = projectile.velocity;
                diff.Normalize();
                projectile.velocity = diff;
                //projectile.direction = projectile.velocity.X > projectile.position.X ? 1 : -1;
                projectile.netUpdate = true;
            }
            int dir = projectile.direction;
        }

		private void CastLights()
		{
			// Cast a light along the line of the laser
			DelegateMethods.v3_1 = new Vector3(0.8f, 0.8f, 1f);
			Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * (Distance - MOVE_DISTANCE), 26, DelegateMethods.CastLight);
		}

		public override bool ShouldUpdatePosition() => false;

		/*
		 * Update CutTiles so the laser will cut tiles (like grass)
		 */
		public override void CutTiles()
		{
			DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
			Vector2 unit = projectile.velocity;
			Utils.PlotTileLine(projectile.Center, projectile.Center + unit * Distance, (projectile.width + 16) * projectile.scale, DelegateMethods.CutTiles);
		}
	}
}
