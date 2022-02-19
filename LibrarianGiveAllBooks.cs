using System;
using System.Collections.Generic;
using ConsoleLib.Console;
using Qud.API;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.World.Conversations;
using XRL.World.Conversations.Parts;

namespace GiveAllBooks.Conversations.Parts
{
	public class LibrarianGiveAllBooks : IConversationPart
	{
		public override bool WantEvent(int ID, int Propagation)
		{
			if (!base.WantEvent(ID, Propagation) && ID != GetChoiceTagEvent.ID)
			{
				return ID == EnterElementEvent.ID;
			}
			return true;
		}

		public static bool IsAwardableBook(GameObject Object)
		{
			if (Object.HasIntProperty("LibrarianAwarded"))
			{
				return false;
			}
			if (Object.HasPart("Book"))
			{
				return true;
			}
			if (Object.HasPart("VillageHistoryBook"))
			{
				return true;
			}
			if (Object.HasPart("MarkovBook"))
			{
				return true;
			}
			if (Object.HasPart("Cookbook"))
			{
				return true;
			}
			return false;
		}

		public override bool HandleEvent(GetChoiceTagEvent E)
		{
			E.Tag = "{{g|[Give All Books]}}";
			return false;
		}

		public override bool HandleEvent(EnterElementEvent E)
		{
			Inventory inventory = The.Player.Inventory;
			int bookCount = 0;	
			int xpTotal = 0;
			string displayName = The.Speaker.GetDisplayName(int.MaxValue, null, null, AsIfKnown: false, Single: false, NoConfusion: true, NoColor: false, Stripped: true, ColorOnly: false, WithoutEpithet: true, Short: false, BaseOnly: true);
			string definiteArticle = The.Speaker.DefiniteArticle(capital: true, displayName, forBase: true);
			string indefiniteArticle = The.Speaker.IndefiniteArticle(capital: true, displayName, forBase: true);
			foreach (GameObject currentObject in inventory.GetObjects(IsAwardableBook))
			{
				if (currentObject.IsImportant())
				{
					continue;
				}
				if (!inventory.FireEvent(Event.New("CommandRemoveObject", "Object", currentObject)))
				{
					continue;
				}
				bookCount += currentObject.Count;
				int xpAward = (int)(currentObject.ValueEach * currentObject.ValueEach / 25.0);
				xpTotal += currentObject.Count * xpAward;
				if(!IComponent<GameObject>.TerseMessages)
				{
					for (int i = 0; i < currentObject.Count; i++)
					{
						Popup.Show("Sheba Hagadias provides some insightful commentary on '" + currentObject.DisplayNameSingle + "'.");
						Popup.Show("You gain {{C|" + xpAward + "}} XP.", CopyScrap: true, Capitalize: true, DimBackground: true, LogMessage: false);
						JournalAPI.AddAccomplishment(indefiniteArticle + displayName + " provided you with insightful commentary on " + currentObject.DisplayName + ".", "Remember the kindness of =name=, who patiently taught " + currentObject.DisplayName + " to " + The.Player.its + " simple pupil, " + indefiniteArticle + displayName + ".", "general", JournalAccomplishment.MuralCategory.LearnsSecret, JournalAccomplishment.MuralWeight.Low, null, -1L);
					}
				}
				if (The.Speaker != null)
				{
					The.Speaker.ReceiveObject(currentObject);
				}
				else
				{
					currentObject.Destroy();
				}
				currentObject.SetIntProperty("LibrarianAwarded", 1);
			}
			if (bookCount == 0)
			{
				Popup.Show("You don't have any suitable books to give.");
				return false;
			}
			if(IComponent<GameObject>.TerseMessages)
			{
				Popup.Show("You give " + definiteArticle + displayName + " " + bookCount + " books, and gain {{C|" + xpTotal + "}} XP.", CopyScrap: true, Capitalize: true, DimBackground: true);
			}
			The.Player.AwardXP(xpTotal, -1, 0, int.MaxValue, null, The.Speaker);
			return base.HandleEvent(E);
		}
	}
}
