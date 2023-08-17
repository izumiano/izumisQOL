using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL
{
	public static class Extensions
	{
		public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description)
		{
			TextMenuExt.EaseInSubHeaderExt descriptionText = new TextMenuExt.EaseInSubHeaderExt(description, initiallyVisible: false, containingMenu)
			{
				TextColor = Color.Gray,
				HeightExtra = 0f
			};
			subMenu.Add(descriptionText);
			subMenuItem.OnEnter = (Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate
			{
				descriptionText.FadeVisible = true;
			});
			subMenuItem.OnLeave = (Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate
			{
				descriptionText.FadeVisible = false;
			});
		}

		public static void NeedsRelaunch(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem)
		{
			TextMenuExt.EaseInSubHeaderExt needsRelaunchText = new TextMenuExt.EaseInSubHeaderExt(Dialog.Clean("MODOPTIONS_NEEDSRELAUNCH"), initiallyVisible: false, containingMenu)
			{
				TextColor = Color.OrangeRed,
				HeightExtra = 0f
			};
			subMenu.Add(needsRelaunchText);
			subMenuItem.OnEnter = (Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate
			{
				needsRelaunchText.FadeVisible = true;
			});
			subMenuItem.OnLeave = (Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate
			{
				needsRelaunchText.FadeVisible = false;
			});
		}
	}
}
