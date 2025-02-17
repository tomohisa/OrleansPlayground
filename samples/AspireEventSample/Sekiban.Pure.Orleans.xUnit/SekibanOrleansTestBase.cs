using Microsoft.Extensions.DependencyInjection;
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
using Xunit;
namespace Sekiban.Pure.Orleans.xUnit;

public abstract class SekibanOrleansTestBase<TDomainTypesGetter> : ISiloConfigurator, IAsyncLifetime
    where TDomainTypesGetter : ISiloConfigurator, new()
{
    /// <summary>
    ///     Each test case implements domain types through this abstract method.
    /// </summary>
    private SekibanDomainTypes _domainTypes => GetDomainTypes();

    public abstract SekibanDomainTypes GetDomainTypes();

    private ICommandMetadataProvider _commandMetadataProvider;
    private ISekibanExecutor _executor;
    private TestCluster _cluster;
    private Repository _repository;

    public SekibanOrleansTestBase() =>
        _repository = new Repository();

    public async Task InitializeAsync()
    {
        _commandMetadataProvider = new FunctionCommandMetadataProvider(() => "test");
        _repository = new Repository();
        var builder = new TestClusterBuilder();
        builder.Options.InitialSilosCount = 1;
        builder.Options.ClusterId = "dev"; // 固定のクラスタ ID
        builder.Options.ServiceId = "TestService"; // 固定のサービス ID
        builder.AddSiloBuilderConfigurator<TDomainTypesGetter>();
        _cluster = builder.Build();
        _cluster.Deploy();
        _executor = new SekibanOrleansExecutor(_cluster.Client, _domainTypes, _commandMetadataProvider);
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Tear down phase – equivalent to NUnit’s [TearDown]
        _cluster.StopAllSilos();
        await Task.CompletedTask;
    }

    /// <summary>
    ///     Execute command in Given phase.
    /// </summary>
    protected Task<ResultBox<CommandResponse>> GivenCommand(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null) =>
        _executor.CommandAsync(command, relatedEvent);

    /// <summary>
    ///     Execute command in When phase.
    /// </summary>
    protected Task<ResultBox<CommandResponse>> WhenCommand(
        ICommandWithHandlerSerializable command,
        IEvent? relatedEvent = null) =>
        _executor.CommandAsync(command, relatedEvent);

    /// <summary>
    ///     Get aggregate in Then phase.
    /// </summary>
    protected Task<ResultBox<Aggregate>> ThenGetAggregate<TAggregateProjector>(PartitionKeys partitionKeys)
        where TAggregateProjector : IAggregateProjector, new() =>
        _executor.LoadAggregateAsync<TAggregateProjector>(partitionKeys);

    protected Task<ResultBox<TResult>> ThenQuery<TResult>(IQueryCommon<TResult> query) where TResult : notnull =>
        _executor.QueryAsync(query);

    protected Task<ResultBox<ListQueryResult<TResult>>> ThenQuery<TResult>(IListQueryCommon<TResult> query)
        where TResult : notnull =>
        _executor.QueryAsync(query);

    protected async Task<ResultBox<TMultiProjector>> ThenGetMultiProjector<TMultiProjector>()
        where TMultiProjector : IMultiProjector<TMultiProjector>
    {
        var projector = _cluster.Client.GetGrain<IMultiProjectorGrain>(TMultiProjector.GetMultiProjectorName());
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
        siloBuilder
            .AddMemoryStreams("EventStreamProvider")
            .AddMemoryGrainStorage("EventStreamProvider");
        siloBuilder.ConfigureServices(
            services =>
            {
                services.AddSingleton(_domainTypes);
                services.AddSingleton(_repository);
                services.AddTransient<IEventWriter, InMemoryEventWriter>();
                services.AddTransient<IEventReader, InMemoryEventReader>();
                // Additional services can be registered here.
            });
    }
}