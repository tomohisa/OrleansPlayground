using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans.TestingHost;
using ResultBoxes;
using Sekiban.Pure.Aggregates;
using Sekiban.Pure.Command.Executor;
using Sekiban.Pure.Command.Handlers;
using Sekiban.Pure.Documents;
using Sekiban.Pure.Events;
using Sekiban.Pure.Executors;
using Sekiban.Pure.Orleans.Parts;
using Sekiban.Pure.Projectors;
using Sekiban.Pure.Query;
using Sekiban.Pure.Repositories;
namespace Sekiban.Pure.Orleans.NUnit;

[TestFixture]
public abstract class SekibanOrleansTestBase<TDomainTypesGetter> : ISiloConfigurator
    where TDomainTypesGetter : ISiloConfigurator, new()
{
    /// <summary>
    ///     Each test case implements domain types through this abstract property
    /// </summary>
    private SekibanDomainTypes _domainTypes => GetDomainTypes();

    public abstract SekibanDomainTypes GetDomainTypes();

    private ICommandMetadataProvider _commandMetadataProvider;
    private IServiceProvider _serviceProvider;
    private ISekibanExecutor _executor;
    private TestCluster _cluster;
    private readonly Repository _repository = new();

    [SetUp]
    public virtual void SetUp()
    {
        _commandMetadataProvider = new FunctionCommandMetadataProvider(() => "test");
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TDomainTypesGetter>();
        _cluster = builder.Build();
        _cluster.Deploy();

        _serviceProvider = _cluster.ServiceProvider;
        _executor = new SekibanOrleansExecutor(_cluster.Client, _domainTypes, _commandMetadataProvider);
    }

    [TearDown]
    public virtual void TearDown()
    {
        _cluster.StopAllSilos();
    }

    /// <summary>
    ///     Execute command in Given phase
    /// </summary>
    protected Task<ResultBox<CommandResponse>> GivenCommand(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null) =>
        _executor.CommandAsync(command, relatedEvent);

    /// <summary>
    ///     Execute command in When phase
    /// </summary>
    protected Task<ResultBox<CommandResponse>> WhenCommand(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null) =>
        _executor.CommandAsync(command, relatedEvent);

    /// <summary>
    ///     Get aggregate in Then phase
    /// </summary>
    protected Task<ResultBox<Aggregate>> ThenGetAggregate<TAggregateProjector>(PartitionKeys partitionKeys)
        where TAggregateProjector : IAggregateProjector, new()
        => _executor.LoadAggregateAsync<TAggregateProjector>(partitionKeys);

    protected Task<ResultBox<TResult>> ThenQuery<TResult>(IQueryCommon<TResult> query) where TResult : notnull
        => _executor.QueryAsync(query);

    protected Task<ResultBox<ListQueryResult<TResult>>> ThenQuery<TResult>(IListQueryCommon<TResult> query)
        where TResult : notnull
        => _executor.QueryAsync(query);

    protected async Task<ResultBox<TMultiProjector>> ThenGetMultiProjector<TMultiProjector>()
        where TMultiProjector : IMultiProjector<TMultiProjector>, new()
    {
        var projector
            = _cluster.Client.GetGrain<IMultiProjectorGrain>(TMultiProjector.GetMultiProjectorName());
        var state = await projector.GetStateAsync();
        var typed = _domainTypes.MultiProjectorsType.ToTypedState(state);
        if (typed is MultiProjectionState<TMultiProjector> multiProjectionState)
        {
            return multiProjectionState.Payload;
        }
        return ResultBox<TMultiProjector>.Error(new ApplicationException("Invalid state"));
    }
    public virtual void Configure(ISiloBuilder siloBuilder)
    {
        siloBuilder.AddMemoryGrainStorage("PubSubStore");
        siloBuilder.AddMemoryGrainStorageAsDefault();
        siloBuilder.AddMemoryStreams("EventStreamProvider").AddMemoryGrainStorage("EventStreamProvider");
        siloBuilder.ConfigureServices(
            services =>
            {
                services.AddSingleton(_domainTypes);
                services.AddSingleton(_repository);
                services.AddTransient<IEventWriter, InMemoryEventWriter>();
                services.AddTransient<IEventReader, InMemoryEventReader>();
                // services.AddTransient()
            });
    }
}