namespace AspireEventSample.ApiService.Grains;

using AspireEventSample.ApiService.Aggregates.ReadModel;
using Orleans;

public interface IBranchEntityWriter : IEntityWriter<BranchEntity>, IGrainWithStringKey
{
}
