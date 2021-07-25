using HarmonyLib;
using System;
using Qud.API;
using XRL;
using XRL.World;
using XRL.World.Parts;
using XRL.UI;

namespace GiveAllBooks.HarmonyPatches
{
	[HarmonyPatch(typeof(ConversationChoice), "GetDisplayText")]
	class DisplayTextPatch
	{
		static bool Prefix(ref ConversationChoice __instance, ref string __result)
		{
			if(__instance.SpecialRequirement == "GiveAllBooks")
			{
				__result = __instance.Text + " {{g|[Give All Books]}}";
				return false; // skip the original method
			}
			return true; // otherwise do the default
		}
	}

	[HarmonyPatch(typeof(ConversationChoice), "CheckSpecialRequirements")]
	class CheckSpecialRequirementsPatch
	{
		static void Postfix(ref ConversationChoice __instance, ref bool __result, GameObject Speaker, GameObject Player)
		{
			if(!__result)
			{
				return; // we already did another requirement and failed
			}
			// this should always be exclusive with the other requirements, since it's an equality check
			// then again, someone might do something weird with operator overloading or whatever
			// so if this causes issues, it's probably that
			if(__instance.SpecialRequirement == "GiveAllBooks")
			{
				Inventory inventory = Player.Inventory;
				bool hasBooks = false;
				foreach (GameObject currentObject in inventory.GetObjects())
				{
					if (currentObject.HasIntProperty("LibrarianAwarded")
						|| (!currentObject.HasPart("Book") && !currentObject.HasPart("VillageHistoryBook") && !currentObject.HasPart("MarkovBook") && !currentObject.HasPart("Cookbook"))
						|| currentObject.IsImportant())
					{
						continue;
					}
					currentObject.SplitStack(1, The.Player);
					if (!inventory.FireEvent(Event.New("CommandRemoveObject", "Object", currentObject)))
					{
						continue;
					}
					hasBooks = true;
					Commerce commerce = currentObject.GetPart("Commerce") as Commerce;
					int xpAward = (int)Math.Floor(commerce.Value * commerce.Value / 25.0);
					Popup.Show("Sheba Hagadias provides some insightful commentary on '" + currentObject.DisplayName + "'.");
					Popup.Show("You gain {{C|" + xpAward + "}} XP.", CopyScrap: true, Capitalize: true, DimBackground: true, LogMessage: false);
					The.Player.AwardXP(xpAward, -1, 0, int.MaxValue, null, Speaker);
					JournalAPI.AddAccomplishment("Sheba Hagadias provided you with insightful commentary on " + currentObject.DisplayName + ".", "Remember the kindness of =name=, who patiently taught " + currentObject.DisplayName + " to " + The.Player.GetPronounProvider().PossessiveAdjective + " simple pupil, Sheba Hagadias.", "general", JournalAccomplishment.MuralCategory.LearnsSecret, JournalAccomplishment.MuralWeight.Low, null, -1L);
					if (Speaker != null)
					{
						Speaker.ReceiveObject(currentObject);
					}
					else
					{
						currentObject.Destroy();
					}
					currentObject.SetIntProperty("LibrarianAwarded", 1);
				}
				if (!hasBooks)
				{
					Popup.Show("You don't have any suitable books to give.");
					__result = false;
					return;
				}
			}
		}
	}
}