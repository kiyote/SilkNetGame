using System.Numerics;
using Microsoft.Extensions.DependencyInjection;

namespace GameFramework.Animations;

public static class ExtensionMethods {

	public static IServiceCollection AddTweeningEngine(
		this IServiceCollection services
	) {

		if ( Vector.IsHardwareAccelerated ) {
			services.AddSingleton<ITweeningEngine, VectorTweeningEngine>();
		} else {
			services.AddSingleton<ITweeningEngine, ScalarTweeningEngine>();
		}

		return services;
	}
}
