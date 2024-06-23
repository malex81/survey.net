using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace ImageProcessing.Engine;
public class EngineKernel : IDisposable
{
	public class Builder(IConfigurationRoot config)
	{
		private readonly IConfigurationRoot config = config;
		private readonly ServiceCollection serviceCollection = new();

		public Builder AddServices(Action<IServiceCollection, IConfigurationRoot> configure)
		{
			configure(serviceCollection, config);
			return this;
		}

		public Builder AddServices(Action<IServiceCollection> configure)
		{
			configure(serviceCollection);
			return this;
		}

		public EngineKernel Build() => new(serviceCollection.BuildServiceProvider(), config);
	}

	public static Builder ConfigureBuilder(Action<ConfigurationBuilder> configure)
	{
		ConfigurationBuilder configBuilder = new();
		configure(configBuilder);
		return new(configBuilder.Build());
	}

	private readonly ServiceProvider services;

	EngineKernel(ServiceProvider services, IConfigurationRoot config)
	{
		this.services = services;
		AppConfig = config;
	}

	public IServiceProvider Services => services;
	public IConfigurationRoot AppConfig { get; }

	public void Dispose()
	{
		services.Dispose();
		GC.SuppressFinalize(this);
	}
}