using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Celeste.Mod.izumisQOL.Scripts;
using Monocle;
using Microsoft.Xna.Framework.Input;
using Celeste.Mod;

namespace Celeste.Mod.izumisQOL;

public static class KeybindViewer{
	public static void Load()
	{
		var folder = Path.Combine(nameof(izumisQOL), "KeybindViewer");
		Directory.CreateDirectory(folder);

		CopyAsset("index.html");
		CopyAsset("main.js");
		CopyAsset("styles.css");
	}

	private static void CopyAsset(string fileName)
	{
		Log("Copying assets for keybindViewer...");
		
		var fileRelativePath = Path.Combine("KeybindViewer", fileName);
		var assetPath        = Path.Combine("Assets",        fileRelativePath);
		if( !Everest.Content.TryGet(assetPath, out ModAsset? htmlAsset, includeDirs: true) || htmlAsset is null )
		{
			Log($"Failed to load {assetPath}", LogLevel.Error);
			return;
		}

		using FileStream fileStream = File.Create(Path.Combine(nameof(izumisQOL), fileRelativePath));
		htmlAsset.Stream.Seek(0L, SeekOrigin.Begin);
		htmlAsset.Stream.CopyTo(fileStream);
		Log($"Wrote file '{fileName}' to path '{fileRelativePath}'");
	}
	
	public static void Show()
	{
		Process.Start(new ProcessStartInfo(@$"{nameof(izumisQOL)}\KeybindViewer\index.html")
    {
    	UseShellExecute = true
    });
	}

	public static BindingCollection GetBindInfo()
	{
		var bindings = new BindingCollection();
		foreach( EverestModule module in Everest.Modules )
		{
			object settings = module._Settings;
			if( settings is null )
			{
				Log($"Could not get modsettings for {module.Metadata.Name}.", LogLevel.Warn);
				continue;
			}

			Log(settings);

			PropertyInfo[] properties = module.SettingsType.GetProperties();
			foreach( PropertyInfo prop in properties )
			{
				SettingInGameAttribute? attribInGame;
				if( ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) is not null &&
				     attribInGame.InGame != Engine.Scene is Level)                ||
				    prop.GetCustomAttribute<SettingIgnoreAttribute>() is not null || !prop.CanRead || !prop.CanWrite )
				{
					continue;
				}

				if( typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType) )
				{
					if( prop.GetValue(settings) is ButtonBinding )
					{
						var           binding           = (ButtonBinding?)prop.GetValue(settings);
						if( binding?.Button?.Binding is null )
						{
							Log("binding was null");
							continue;
						}
						bindings.Add(new BindInfo(module.Metadata.Name, binding.Button.Binding));
					}

					continue;
				}

				if( typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType) )
				{
					if( prop.GetValue(settings) is List<ButtonBinding> )
					{
						List<ButtonBinding>? buttonBindingList = (List<ButtonBinding>?)prop.GetValue(settings) ?? [];
						// buttonBindingList?.ForEach(buttonBinding =>
						foreach( ButtonBinding binding in buttonBindingList )
						{
							if( binding.Button?.Binding is not null )
							{
								bindings.Add(new BindInfo(module.Metadata.Name, binding.Button.Binding));
							}
							else
							{
								Log("binding in list was null");
							}
						}
					}
				}
			}
		}

		return bindings;
	}
}

public record BindInfo(string Module, Binding Binding);

public class BindingCollection
{
	public Dictionary<Keys, List<BindInfo>>    Keybinds = new();
	public Dictionary<Buttons, List<BindInfo>> Buttons  = new();

	public void Add(BindInfo bindInfo)
	{
		bindInfo.Binding.Keyboard.ForEach(key =>
		{
			if( Keybinds.TryGetValue(key, out List<BindInfo>? binds) )
			{
				binds.Add(bindInfo);
			}
			else
			{
				Keybinds.Add(key, [ bindInfo ]);
			}
		});

		bindInfo.Binding.Controller.ForEach(button =>
		{
			if( Buttons.TryGetValue(button, out List<BindInfo>? binds) )
			{
				binds.Add(bindInfo);
			}
			else
			{
				Buttons.Add(button, [ bindInfo ]);
			}
		});
	}

	public override string ToString()
	{
		return
			Keybinds.Aggregate("\n", (current, bind) =>
				current
				+ $"{bind.Key}: {LogParser.Array(bind.Value.Select(bindInfo => bindInfo.Module).ToArray())}"
				+ "\n");
	}
}