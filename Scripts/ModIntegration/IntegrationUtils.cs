using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.izumisQOL.ModIntegration
{
	public class IntegrationUtils
	{
		public static bool TryGetModule(EverestModuleMetadata meta, out EverestModule module)
		{
			foreach (EverestModule other in Everest.Modules)
			{
				EverestModuleMetadata otherData = other.Metadata;
				if (!(otherData.Name != meta.Name))
				{
					Version version = otherData.Version;
					if (Everest.Loader.VersionSatisfiesDependency(meta.Version, version))
					{
						module = other;
						return true;
					}
				}
			}
			module = null;
			return false;
		}
	}
}
