using System.Collections.Generic;
using System.Reflection;
using Celeste;
using Celeste.Mod;
using Monocle;

namespace Celeste.Mod.izumisQOL.Menu
{
	public class CustomModuleSettingsButtonConfigUI : ModuleSettingsButtonConfigUI
	{
		public CustomModuleSettingsButtonConfigUI(EverestModule module)
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
			Add(new Header("BTN_CONFIG_TITLE".AsDialog()));
			Add(new InputMappingInfo(controllerMode: true));
			Bindings.Clear();
			object settings = Module._Settings;
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
						string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? prop.Name;
						name = name.DialogCleanOrNull() ?? (name.ToLowerInvariant().StartsWith("button") ? name.Substring(6) : name).SpacedPascalCase();
						DefaultButtonBindingAttribute defaults = prop.GetCustomAttribute<DefaultButtonBindingAttribute>();
						Bindings.Add(new ButtonBindingEntry(binding, defaults));
						string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
						if (subheader != null)
						{
							Add(new SubHeader(subheader.DialogCleanOrNull() ?? subheader));
						}
						AddMapForceLabel(name, binding.Binding);
					}
				}
				else
				{
					if (!(prop.GetValue(settings) is List<ButtonBinding> bindingMap))
					{
						continue;
					}
					string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? prop.Name;
					name = name.DialogCleanOrNull() ?? (name.ToLowerInvariant().StartsWith("buttons") ? name.Substring(7) : name).SpacedPascalCase();

					string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
					if (subheader != null)
					{
						Add(new SubHeader(subheader.DialogCleanOrNull() ?? subheader));
					}
					for(int i = 0; i < bindingMap.Count; i++)
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
