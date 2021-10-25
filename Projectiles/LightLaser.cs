using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.ID;

namespace LightAscend.Projectiles
{
	// The following laser shows a channeled ability, after charging up the laser will be fired
	// Using custom drawing, dust effects, and custom collision checks for tiles
	public class LightLaser : ModProjectile
	{
		// Use a different style for constant so it is very clear in code when a constant is used

		// The maximum charge value
		private const float MAX_CHARGE = 5f;
		//The distance charge particle from the player center
		private const float MOVE_DISTANCE = 60f;

		// The actual distance is stored in the ai0 field
		// By making a property to handle this it makes our life easier, and the accessibility more readable
		public float Distance
		{
			get => projectile.ai[0];
			set => projectile.ai[0] = value;
		}
        //prismlike
        //private float NextManaFrame
        //{
        //	get => projectile.ai[1];
        //	set => projectile.ai[1] = value;
        //}
        // The actual charge value is stored in the localAI0 field
        public float Charge
		{
			get => projectile.localAI[0];
			set => projectile.localAI[0] = value;
		}
		public float ManaConsumeTimer
		{
			get => projectile.ai[1];
			set => projectile.ai[1] = value;
		}

		// This value controls how frequently the Prism emits sound once it's firing.
		private const int SoundInterval = 20;

		// Are we at max charge? With c#6 you can simply use => which indicates this is a get only property
		public bool IsAtMaxCharge => Charge == MAX_CHARGE;
		public override void SetStaticDefaults()
		{
			// Signals to Terraria that this projectile requires a unique identifier beyond its index in the projectile array.
			// This prevents the issue with the vanilla Last Prism where the beams are invisible in multiplayer.
			ProjectileID.Sets.NeedsUUID[projectile.type] = true;
		}
		public override void SetDefaults()
		{
			projectile.width = 10;
			projectile.height = 10;
			projectile.friendly = true;
			projectile.penetrate = -1;
			projectile.tileCollide = false;
			projectile.magic = true;
			projectile.hide = true;
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
		{
			// We start drawing the laser if we have charged up
			if (IsAtMaxCharge)
			{
				DrawLaser(spriteBatch, Main.projectileTexture[projectile.type], Main.player[projectile.owner].Center,
					projectile.velocity, 10, projectile.damage, -1.57f, 1f, 1000f, Color.White, (int)MOVE_DISTANCE);
			}
			return false;
		}

		// The core function of drawing a laser
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

		// Change the way of collision check of the projectile
		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			// We can only collide if we are at max charge, which is when the laser is actually fired
			if (!IsAtMaxCharge) return false;

			Player player = Main.player[projectile.owner];
			Vector2 unit = projectile.velocity;
			float point = 0f;
			// Run an AABB versus Line check to look for collisions, look up AABB collision first to see how it works
			// It will look for collisions on the given line using AABB
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), player.Center,
				player.Center + unit * Distance, 22, ref point);
		}

		// Set custom immunity time on hitting an NPC
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.immune[projectile.owner] = 5;
			if (Main.rand.NextBool(65)) //the larger the number, the less chance 
			{
				SpawnShinigStar(target.Center, projectile.damage, 4f);
			}
		}
		private void SpawnShinigStar(Vector2 spawnPoint, int damage, float knockback)
        {
			int uuid = Projectile.GetByUUID(projectile.owner, projectile.whoAmI);
			Projectile.NewProjectile(spawnPoint, new Vector2(0,0), ModContent.ProjectileType<ShiningStar>(), damage, knockback, projectile.owner, 0, 0);
			projectile.netUpdate = true;
		}
		// The AI of the projectile
		public override void AI()
		{
			Player player = Main.player[projectile.owner];
			projectile.position = player.Center + projectile.velocity * MOVE_DISTANCE;
			projectile.timeLeft = 2;

			// By separating large AI into methods it becomes very easy to see the flow of the AI in a broader sense
			// First we update player variables that are needed to channel the laser
			// Then we run our charging laser logic
			// If we are fully charged, we proceed to update the laser's position
			// Finally we spawn some effects like dusts and light
			ConsumeMana(player);
			UpdatePlayer(player);
			ChargeLaser(player);

			// If laser is not charged yet, stop the AI here.
			if (Charge < MAX_CHARGE) return;

			SetLaserPosition(player);
			SpawnDusts(player);
			CastLights();
			PlaySounds();
			//prismlike
			//// Update the Prism's behavior: project beams on frame 1, consume mana, and despawn if out of mana.
			//if (projectile.owner == Main.myPlayer)
			//{
			//	// player.CheckMana returns true if the mana cost can be paid. Since the second argument is true, the mana is actually consumed.
			//	// If mana shouldn't consumed this frame, the || operator short-circuits its evaluation player.CheckMana never executes.
			//	bool manaIsAvailable = !ShouldConsumeMana() || player.CheckMana(player.HeldItem.mana, true, false);

			//	// The Prism immediately stops functioning if the player is Cursed (player.noItems) or "Crowd Controlled", e.g. the Frozen debuff.
			//	// player.channel indicates whether the player is still holding down the mouse button to use the item.
			//	bool stillInUse = player.channel && manaIsAvailable && !player.noItems && !player.CCed;

			//	// Spawn in the Prism's lasers on the first frame if the player is capable of using the item.
			//	if (stillInUse && FrameCounter == 1f)
			//	{
			//		FireBeam();
			//	}

			//	//// If the Prism cannot continue to be used, then destroy it immediately.
			//	//else if (!stillInUse)
			//	//{
			//	//	projectile.Kill();
			//	//}
			//}
		}
		#region prismlike
		//      private void FireBeam()
		//{
		//	// This UUID will be the same between all players in multiplayer, ensuring that the beams are properly anchored on the Prism on everyone's screen.
		//	int uuid = Projectile.GetByUUID(projectile.owner, projectile.whoAmI);

		//	// After creating the beams, mark the Prism as having an important network event. This will make Terraria sync its data to other players ASAP.
		//	projectile.netUpdate = true;
		//}
		//private bool ShouldConsumeMana()
		//{
		//	// If the mana consumption timer hasn't been initialized yet, initialize it and consume mana on frame 1.
		//	NextManaFrame = MaxManaConsumptionDelay;

		//	// Should mana be consumed this frame?
		//	bool consume = FrameCounter == NextManaFrame;

		//	NextManaFrame ++;

		//	return consume;
		//}
		#endregion
		private void SpawnDusts(Player player)
		{
			Vector2 unit = projectile.velocity * -1;
			Vector2 dustPos = player.Center + projectile.velocity * Distance * 1.007f;

            for (int i = 0; i < 2; ++i)
            {
				float velMult = 1f;
                float num1 = projectile.velocity.ToRotation() + (Main.rand.Next(2) == 1 ? -1.0f : 1.0f) * 1.57f;
                float num2 = (float)(Main.rand.NextDouble() * 0.8f + 1.0f);
                Vector2 dustVel = new Vector2((float)Math.Cos(num1) * num2, (float)Math.Sin(num1) * num2) * velMult;
                Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, DustID.AncientLight, dustVel.X, dustVel.Y)];
                dust.noGravity = true;
                dust.scale = 2f;
				dust.color = Color.White;
            }
        }
		private void ConsumeMana(Player player)
        {
			ManaConsumeTimer++;
			if (ManaConsumeTimer>=6)
            {
				ManaConsumeTimer = 0;
                if (!player.CheckMana(player.HeldItem.mana, true, false))
                {
					projectile.Kill();
                }
			}
		}
		
		private void PlaySounds()
		{
			// The Prism makes sound intermittently while in use, using the vanilla projectile variable soundDelay.
			if (projectile.soundDelay <= 0)
			{
				projectile.soundDelay = SoundInterval;

				// On the very first frame, the sound playing is skipped. This way it doesn't overlap the starting hiss sound.
				if (IsAtMaxCharge)
				{
					Main.PlaySound(SoundID.Item15, projectile.position);
				}
			}
		}
		/*
		 * Sets the end of the laser position based on where it collides with something
		 */
		private void SetLaserPosition(Player player)
		{
			for (Distance = MOVE_DISTANCE; Distance <= 2200f; Distance += 5f)
			{
				var start = player.Center + projectile.velocity * Distance;
				//if (!Collision.CanHit(player.Center, 1, 1, start, 1, 1))
				if (!Collision.CanHitLine(player.Center, 1, 1, start, 1, 1) && !Collision.CanHit(player.Center, 1, 1, start, 1, 1))
				{
					Distance -= 5f;
					break;
				}
			}
		}

		private void ChargeLaser(Player player)
		{
			// Kill the projectile if the player stops channeling
			if (!player.channel)
			{
				projectile.Kill();
			}
			else
			{
				// Do we still have enough mana? If not, we kill the projectile because we cannot use it anymore
				if (Main.time % 10 < 1 && !player.CheckMana(player.inventory[player.selectedItem].mana, true))
				{
					projectile.Kill();
				}
				Vector2 offset = projectile.velocity;
				offset *= MOVE_DISTANCE - 20;
				Vector2 pos = player.Center + offset - new Vector2(10, 10);
				if (Charge < MAX_CHARGE)
				{
					Charge++;
				}
				int chargeFact = (int)(Charge / 20f);
				Vector2 dustVelocity = Vector2.UnitX * 18f;
				dustVelocity = dustVelocity.RotatedBy(projectile.rotation - 1.57f);
				Vector2 spawnPos = projectile.Center + dustVelocity;
				for (int k = 0; k < chargeFact + 1; k++)
				{
					Vector2 spawn = spawnPos + ((float)Main.rand.NextDouble() * 6.28f).ToRotationVector2() * (12f - chargeFact * 2);
					Dust dust = Main.dust[Dust.NewDust(pos, 20, 20, DustID.AncientLight, projectile.velocity.X, projectile.velocity.Y)];
					dust.velocity = Vector2.Normalize(spawnPos - spawn) * 1.5f * (10f - chargeFact * 2f) / 10f;
					dust.noGravity = true;
					dust.scale = Main.rand.Next(10, 20) * 0.1f;
				}
			}
		}
		
		private void UpdatePlayer(Player player)
		{
			// Multiplayer support here, only run this code if the client running it is the owner of the projectile
			if (projectile.owner == Main.myPlayer)
			{
				Vector2 diff = Main.MouseWorld - player.Center;
				diff.Normalize();
				projectile.velocity = diff;
				projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
				projectile.netUpdate = true;
			}
			int dir = projectile.direction;
			player.ChangeDir(dir); // Set player direction to where we are shooting
			player.heldProj = projectile.whoAmI; // Update player's held projectile
			player.itemTime = 2; // Set item time to 2 frames while we are used
			player.itemAnimation = 2; // Set item animation time to 2 frames while we are used
			player.itemRotation = (float)Math.Atan2(projectile.velocity.Y * dir, projectile.velocity.X * dir); // Set the item rotation to where we are shooting
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
