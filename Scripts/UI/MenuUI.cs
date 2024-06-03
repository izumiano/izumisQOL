using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL.UI;
public class General
{
	public static void OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons)
	{
		if (!ModSettings.ShowRestartButtonInMainMenu)
			return;

		MainMenuSmallButton btn = new("MODOPTIONS_IZUMISQOL_RESTART", "menu/restart", menu, Vector2.Zero, Vector2.Zero,
			delegate
			{
				Everest.QuickFullRestart();
			}
		);
		buttons.Add(btn);
	}
}

public interface IToggleableMenuItem
{
	public bool IsShown { get; set; }

	public void Show(int index, TextMenu menu, TextMenuExt.SubMenu subMenu);
}

public class ToggleableButton : TextMenu.Button, IToggleableMenuItem
{
	protected static Dictionary<string, bool> IsShownFromID = new();

	public bool IsShown 
	{
		get
		{
			if (!IsShownFromID.ContainsKey(id))
			{
				IsShownFromID.Add(id, false);
				return false;
			}
			return IsShownFromID[id];
		}
		set
		{
			IsShownFromID[id] = value;
		}
	}
	protected string id;
	protected string description;

	protected ToggleableButton(string label, string id, string description, bool visibleByDefault = false) : base(label) 
	{
		this.id = id;
		this.description = description;
		if (!IsShownFromID.ContainsKey(id))
		{
			IsShownFromID.Add(id, visibleByDefault);
		}
	}

	public static ToggleableButton New(string label, string id, string description, bool visibleByDefault = false)
	{
		ToggleableButton btn = new(label, id, description, visibleByDefault)
		{
			IsShown = IsShownFromID.ContainsKey(id) ? IsShownFromID[id] : visibleByDefault
		};
		return btn;
	}

	public void Show(int index, TextMenu menu, TextMenuExt.SubMenu subMenu)
	{
		if (!IsShown)
		{
			IsShown = true;
			subMenu.Insert(index, this);
			subMenu.InsertDescription(menu, this, description);
			SelectWiggler.Start();
		}
	}

	public void AddToMenuIfIsShown(TextMenu menu, TextMenuExt.SubMenu subMenu)
	{
		if (IsShown)
		{
			subMenu.Add(this);
			subMenu.AddDescription(menu, this, description);
		}
	}
}

public class ToggleableRestartButton : ToggleableButton
{
	private ToggleableRestartButton(string id, bool visibleByDefault = false) : base("MODOPTIONS_IZUMISQOL_RESTART".AsDialog(), id, "MODOPTIONS_IZUMISQOL_RESTARTDESC".AsDialog(), visibleByDefault) 
	{
		OnPressed = delegate
		{
			izumisQOL.Instance.SaveSettings();
			Everest.QuickFullRestart();
		};
	}

	public static ToggleableRestartButton New(string id, bool visibleByDefault = false)
	{
		ToggleableRestartButton btn = new(id)
		{
			IsShown = IsShownFromID.ContainsKey(id) ? IsShownFromID[id] : visibleByDefault
		};
		return btn;
	}
}

public class DisableableButton : TextMenu.Button
{
	Func<bool> shouldDisable;

	public DisableableButton(string label, Func<bool> shouldDisable) : base(label) 
	{
		this.shouldDisable = shouldDisable;
	}

	public override void Update()
	{
		base.Update();
		Disabled = shouldDisable();
	}
}
