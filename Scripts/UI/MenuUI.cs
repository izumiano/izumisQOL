using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL.UI;

public class General
{
	public static void OnCreateButtons(OuiMainMenu menu, List<MenuButton> buttons)
	{
		if( !ModSettings.ShowRestartButtonInMainMenu )
			return;

		MainMenuSmallButton btn = new("MODOPTIONS_IZUMISQOL_RESTART", "menu/restart", menu, Vector2.Zero, Vector2.Zero,
			delegate { Everest.QuickFullRestart(); }
		);
		buttons.Add(btn);
	}
}

public interface IToggleableMenuItem
{
	public bool IsShown { get; set; }

	public void Show(TextMenu menu, TextMenuExt.SubMenu subMenu);
}

public class ToggleableButton : TextMenu.Button, IToggleableMenuItem
{
	protected static readonly Dictionary<string, bool> IsShownFromID = new();

	public bool IsShown
	{
		get
		{
			if( !IsShownFromID.ContainsKey(id) )
			{
				IsShownFromID.Add(id, false);
				return false;
			}

			return IsShownFromID[id];
		}
		set
		{
			Visible           = value;
			IsShownFromID[id] = value;
		}
	}

	protected readonly string id;
	protected readonly string description;
	protected readonly Color  RegularColor;

	protected ToggleableButton(string label, string id, string description, Color? regularColor = null, bool visibleByDefault = false) : base(label)
	{
		this.id          = id;
		this.description = description;
		if( !IsShownFromID.ContainsKey(id) ) IsShownFromID.Add(id, visibleByDefault);
		IsShown      = visibleByDefault;
		RegularColor = regularColor ?? Color.White;;
	}

	public static ToggleableButton New(string label, string id, string description, Color? regularColor = null, bool visibleByDefault = false)
	{
		ToggleableButton btn = new(label, id, description, regularColor, visibleByDefault)
		{
			IsShown = IsShownFromID.ContainsKey(id) ? IsShownFromID[id] : visibleByDefault,
		};
		return btn;
	}

	public void Show(TextMenu menu, TextMenuExt.SubMenu subMenu)
	{
		IsShown = true;
		SelectWiggler.Start();
	}

	public void AddToMenuIfIsShown(TextMenu menu, TextMenuExt.SubMenu subMenu)
	{
		subMenu.Add(this);
		subMenu.AddDescription(menu, this, description);
	}

	public override void Render(Vector2 position, bool highlighted)
	{
		var alpha       = Container.Alpha;
		var color       = Disabled ? Color.DarkSlateGray : (highlighted ? Container.HighlightColor : RegularColor) * alpha;
		var strokeColor = Color.Black * (alpha * alpha * alpha);
		var flag        = Container.InnerContent == TextMenu.InnerContentMode.TwoColumn && !AlwaysCenter;
		ActiveFont.DrawOutline(Label, position + (flag ? Vector2.Zero : new Vector2(Container.Width * 0.5f, 0.0f)),
			!flag || AlwaysCenter ? new Vector2(0.5f, 0.5f) : new Vector2(0.0f, 0.5f), Vector2.One, color, 2f, strokeColor);
	}
}

public class ToggleableRestartButton : ToggleableButton
{
	private ToggleableRestartButton(string id, bool visibleByDefault = false,  Color? regularColor = null) : base(
		"MODOPTIONS_IZUMISQOL_RESTART".AsDialog(), id, "MODOPTIONS_IZUMISQOL_RESTARTDESC".AsDialog(), regularColor ?? Color.OrangeRed, visibleByDefault)
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
			IsShown = IsShownFromID.ContainsKey(id) ? IsShownFromID[id] : visibleByDefault,
		};
		return btn;
	}
}

public class DisableableButton : TextMenu.Button
{
	private readonly Func<bool> shouldDisable;

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