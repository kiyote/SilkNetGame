using Game.Framework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SilkNetGame;

public static class Program {

	public const string Title = "Silk.Net Test";

	public static void Main(
		string[] args
	) {
		IConfiguration configuration = BuildConfiguration( args );

		DisplayOptions displayOptions = new DisplayOptions();
		configuration.GetSection( DisplayOptions.Section ).Bind( displayOptions );

		Device device = new Device(
			Title,
			displayOptions.Width,
			displayOptions.Height,
			displayOptions.VSync,
			displayOptions.Mode
		);
		device
			.ConfigureServices( services => {
				services.AddSingleton( configuration );
				services.AddGame<MyGame>();
			} )
			.Run();
	}

	private static IConfiguration BuildConfiguration(
		string[] args
	) {
		ConfigurationManager configuration = new ConfigurationManager();

		string environment = Environment.GetEnvironmentVariable( "DOTNET_ENVIRONMENT" )
							 ?? Environments.Production;

		configuration
			.AddJsonFile( "appsettings.json", optional: true, reloadOnChange: false )
			.AddJsonFile( $"appsettings.{environment}.json", optional: true, reloadOnChange: false )
			.AddEnvironmentVariables()
			.AddCommandLine( args );

		return configuration;
	}

}
