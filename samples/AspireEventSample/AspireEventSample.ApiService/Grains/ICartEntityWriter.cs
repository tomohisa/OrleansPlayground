using AspireEventSample.ApiService.Aggregates.ReadModel;
using Sekiban.Pure.OrleansEventSourcing;
namespace AspireEventSample.ApiService.Grains;

public interface ICartEntityWriter : IEntityWriter<CartEntity>, IGrainWithStringKey { }