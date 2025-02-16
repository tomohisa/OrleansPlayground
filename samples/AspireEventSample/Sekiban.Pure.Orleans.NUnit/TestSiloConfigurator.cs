using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
namespace Sekiban.Pure.Orleans.NUnit;

public class TestSiloConfigurator<TDomainTypesGetter> : ISiloConfigurator
    where TDomainTypesGetter : IDomainTypesGetter, new()
{
    public void Configure(ISiloBuilder siloBuilder)
    {
        var domainTypes = new TDomainTypesGetter().GetDomainTypes();

        siloBuilder.AddMemoryGrainStorage("PubSubStore");
        siloBuilder.AddMemoryGrainStorageAsDefault();
        siloBuilder.ConfigureServices(
            services =>
            {
                services.AddSingleton(domainTypes);
                // services.AddTransient()
            });
    }
}