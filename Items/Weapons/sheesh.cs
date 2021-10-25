using LightAscend.Projectiles;
//using LightAscend.Sounds;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace LightAscend.Items.Weapons
{
	public class sheesh : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Plasmatic spectre"); // By default, capitalization in classnames will add spaces to the display name. You can customize the display name here by uncommenting this line.
			Tooltip.SetDefault("Spirit rifle with high velocity spectre shots.");
		}

		public override void SetDefaults()
		{
			//item.channel = true; //Channel so that you can hold the weapon [Important]
			item.noMelee = true;
			item.damage = 75;
			item.magic = true;
			item.mana = 4;
			item.width = 40;
			item.height = 20;
			item.useStyle = ItemUseStyleID.HoldingOut;
            if (!Main.dedServ)
            {
				item.UseSound = mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/xdShootSound").WithVolume(3.2f).WithPitchVariance(.25f);
			}
			item.rare = ItemRarityID.Yellow;
			item.autoReuse = true;
			item.useTime = 7;
			item.crit = 25;
			item.useAnimation = 7;
			item.shootSpeed = 5f;
			item.shoot = ModContent.ProjectileType<SheeshProjectile>();
			item.value = Item.sellPrice(platinum:1);
		}
		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.SpectreBar, 4);
			recipe.AddIngredient(ItemID.ShroomiteBar, 12);
			recipe.AddIngredient(ItemID.SoulofMight, 50);
			recipe.AddTile(TileID.MythrilAnvil);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
    }
}