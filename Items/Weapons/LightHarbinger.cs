using LightAscend.Projectiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace LightAscend.Items.Weapons
{
	public class LightHarbinger : ModItem
	{
		public override void SetStaticDefaults()
		{
			Tooltip.SetDefault("The harbinger of the core light.\nSometimes hit enemies shine so bright as star.");
		}

		public override void SetDefaults()
		{
			item.damage = 105;
			item.noMelee = true;
			item.magic = true;
			item.channel = true; //Channel so that you can hold the weapon [Important]
			item.mana = 12;
			item.autoReuse = true;
			item.rare = ItemRarityID.Red;
			item.width = 28;
			item.height = 30;
			item.useTime = 5;
			item.UseSound = SoundID.Item13;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.shootSpeed = 30f;
			item.shoot = ModContent.ProjectileType<LightLaser>();
			item.value = Item.sellPrice(platinum: 1);
		}
		
		public override void AddRecipes()
		{
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.FragmentNebula, 30);
			recipe.AddIngredient(ItemID.FragmentSolar, 30);
			recipe.AddIngredient(ItemID.LastPrism, 1);
			recipe.AddTile(TileID.LunarCraftingStation);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
