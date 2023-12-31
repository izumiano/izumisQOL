using System.Collections.Generic;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Monocle;

namespace Celeste.Mod.izumisQOL.Menu
{
	public class CustomModuleSettingsKeyboardConfigUI : ModuleSettingsKeyboardConfigUI
	{
		public CustomModuleSettingsKeyboardConfigUI(EverestModule module)
			: base(module)
		{
		}

		public override void Reload(int index = -1)
		{
			if (Module == null)
			{
				return;
			}
			Clear();
			Add(new Header("KEY_CONFIG_TITLE".AsDialog()));
			Add(new InputMappingInfo(controllerMode: false));
			object settings = Module._Settings;
			string typeName = Module.SettingsType.Name.ToLowerInvariant();
			if (typeName.EndsWith("settings"))
			{
				typeName = typeName.Substring(0, typeName.Length - 8);
			}
			string nameDefaultPrefix = "modoptions_" + typeName + "_";
			PropertyInfo[] properties = Module.SettingsType.GetProperties();
			foreach (PropertyInfo prop in properties)
			{
				SettingInGameAttribute attribInGame;
				if (((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null && attribInGame.InGame != Engine.Scene is Level) || prop.GetCustomAttribute<SettingIgnoreAttribute>() != null || !prop.CanRead || !prop.CanWrite)
				{
					continue;
				}
				if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType))
				{
					if (prop.GetValue(settings) is ButtonBinding binding)
					{
						string name2 = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? (nameDefaultPrefix + prop.Name.ToLowerInvariant());
						name2 = name2.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("button") ? prop.Name.Substring(6) : prop.Name).SpacedPascalCase();
						DefaultButtonBindingAttribute defaults = prop.GetCustomAttribute<DefaultButtonBindingAttribute>();
						Bindings.Add(new ButtonBindingEntry(binding, defaults));
						string subheader2 = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
						if (subheader2 != null)
						{
							Add(new SubHeader(subheader2.DialogCleanOrNull() ?? subheader2));
						}
						AddMapForceLabel(name2, binding.Binding);
					}
				}
				else
				{
					if (!(prop.GetValue(settings) is List<ButtonBinding> bindingMap))
					{
						continue;
					}
					string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? (nameDefaultPrefix + prop.Name.ToLowerInvariant());
					name = name.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("buttons") ? prop.Name.Substring(7) : prop.Name).SpacedPascalCase();
					
					string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
					if (subheader != null)
					{
						Add(new SubHeader(subheader.DialogCleanOrNull() ?? subheader));
					}
					for(int i = 0; i< bindingMap.Count; i++)
					{
						Bindings.Add(new ButtonBindingEntry(bindingMap[i], null));
						AddMapForceLabel(name + " to slot " + (i + 1), bindingMap[i].Binding);
					}
				}
			}
			Add(new SubHeader(""));
			Add(new Button("KEY_CONFIG_RESET".AsDialog())
			{
				IncludeWidthInMeasurement = false,
				AlwaysCenter = true,
				OnPressed = delegate
				{
					ResetPressed();
				}
			});
			if (index >= 0)
			{
				Selection = index;
			}
		}
	}
}
