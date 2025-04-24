using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Celeste.Mod.izumisQOL.Scripts;
using Monocle;
using Microsoft.Xna.Framework.Input;

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
		Log($"Copying {fileName} for keybindViewer...");
		
		var fileRelativePath = Path.Combine("KeybindViewer", fileName);
		var assetPath        = Path.Combine("Assets",        fileRelativePath);
		if( !Everest.Content.TryGet(assetPath, out ModAsset? asset, includeDirs: true) || asset is null )
		{
			Log($"Failed to load {assetPath}", LogLevel.Error);
			return;
		}

		using FileStream fileStream = File.Create(Path.Combine(nameof(izumisQOL), fileRelativePath));
		asset.Stream.Seek(0L, SeekOrigin.Begin);
		asset.Stream.CopyTo(fileStream);
		Log($"Wrote file '{fileName}' to path '{fileRelativePath}'");
	}
	
	public static void Show()
	{
		Process.Start(new ProcessStartInfo(@$"{nameof(izumisQOL)}\KeybindViewer\index.html")
    {
    	UseShellExecute = true
    });
	}

	public static BindingCollection GetBindInfoCollection()
	{
		var bindings = new BindingCollection();
		foreach( EverestModule module in Everest.Modules )
		{
			if( module._Settings is not { } settings )
			{
				Log($"Could not get modsettings for {module.Metadata.Name}.", LogLevel.Warn);
				continue;
			}

			Log(module.Metadata.Name);

			PropertyInfo[] properties = module.SettingsType.GetProperties();
			foreach( PropertyInfo prop in properties )
			{
				if( !prop.CanRead )
				{
					continue;
				}

				bindings.Add(GetBindInfoFromProperty(prop, module, settings));
			}
		}

		return bindings;
	}

	private static IEnumerable<BindInfo> GetBindInfoFromProperty(PropertyInfo prop, EverestModule module, EverestModuleSettings settings)
	{
		if( typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType) && prop.GetValue(settings) is ButtonBinding binding )
		{
			if( binding.Button?.Binding is null )
			{
				Log("binding was null");
				yield break;
			}
			yield return new BindInfo(module.Metadata.Name, GetPropertyName(prop, module), binding.Button.Binding);
		}
				
		else if( typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType)
		    && prop.GetValue(settings) is List<ButtonBinding> buttonBindingList)
		{
			for(int i = 0; i < buttonBindingList.Count; i++)
			{
				var listBinding = buttonBindingList[i];
				if( listBinding.Button?.Binding is null )
				{
					Log("binding in list was null");
					continue;
				}
							
				yield return new BindInfo(
					module.Metadata.Name, 
					GetPropertyName(prop, module) + $"[{i}]", 
					listBinding.Button.Binding
				);
			}
		}
		
		else if( typeof(List<Keys>).IsAssignableFrom(prop.PropertyType)
		         && prop.GetValue(settings) is List<Keys> keyList)
		{
			foreach( Keys key in keyList )
			{
				var keyBinding = new Binding();
				keyBinding.Add(key);
				
				yield return new BindInfo(
					module.Metadata.Name, 
					GetPropertyName(prop, module), 
					keyBinding
				);
			}
		}
		
		else if( typeof(List<Buttons>).IsAssignableFrom(prop.PropertyType)
		         && prop.GetValue(settings) is List<Buttons> buttonsList)
		{
			foreach( Buttons button in buttonsList )
			{
				var buttonBinding = new Binding();
				buttonBinding.Add(button);
				
				yield return new BindInfo(
					module.Metadata.Name, 
					GetPropertyName(prop, module), 
					buttonBinding
				);
			}
		}

		else if( typeof(IDictionary).IsAssignableFrom(prop.PropertyType)
		    && prop.GetValue(settings) is IDictionary buttonBindingDict)
		{
			foreach( DictionaryEntry keyValue in buttonBindingDict )
			{
				if( keyValue.Value is not ButtonBinding dictBinding )
				{
					prop.Name.Log("was not a ButtonBinding dict");
					break;
				}
				if( dictBinding.Button?.Binding is null )
				{
					Log("binding in list was null");
					continue;
				}
						
				yield return new BindInfo(
					module.Metadata.Name, 
					GetPropertyName(prop, module) + $"[{keyValue.Key}]", 
					dictBinding.Button.Binding
				);
			}
		}
	}
	
	private static string GetPropertyName(PropertyInfo propertyInfo, EverestModule module)
	{
		string text = module.SettingsType.Name.ToLowerInvariant();
		if (text.EndsWith("settings"))
		{
			text = text[..^8];
		}
		string nameDefaultPrefix = "modoptions_" + text + "_";
		string input             = propertyInfo.GetCustomAttribute<SettingNameAttribute>()?.Name
		                           ?? nameDefaultPrefix + propertyInfo.Name.ToLowerInvariant();
		
		return input.DialogCleanOrNull() ?? propertyInfo.Name.SpacedPascalCase();
	}
}

public record BindInfo(string Module, string PropertyName, Binding Binding);

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

	public void Add(IEnumerable<BindInfo> bindInfos)
	{
		foreach( var bindInfo in bindInfos )
		{
			Add(bindInfo);
		}
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