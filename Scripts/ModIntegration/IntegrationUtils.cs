﻿using System;
using System.Diagnostics.CodeAnalysis;

namespace Celeste.Mod.izumisQOL.ModIntegration;

public static class IntegrationUtils
{
	public static bool TryGetModule(EverestModuleMetadata meta, out EverestModule? module)
	{
		foreach( EverestModule other in Everest.Modules )
		{
			EverestModuleMetadata otherData = other.Metadata;
			if( otherData.Name != meta.Name ) continue;

			Version version = otherData.Version;
			if( !Everest.Loader.VersionSatisfiesDependency(meta.Version, version) ) continue;

			module = other;
			return true;
		}

		module = null;
		return false;
	}
}