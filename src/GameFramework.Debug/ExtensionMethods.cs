using GameFramework.Textures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GameFramework;

public static class ExtensionMethods {

	public static IServiceCollection AddGameFrameworkDebug(
		this IServiceCollection services
	) {
		services.TryAddSingleton<TextureDebug>();
		return services;
	}
}
