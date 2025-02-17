using Sekiban.Pure.Query;
namespace Sekiban.Pure.Orleans.Surrogates;

[RegisterConverter]
public sealed class OrleansListQueryResultGeneralConverter : IConverter<ListQueryResultGeneral, OrleansListQueryResultGeneral>
{
    public ListQueryResultGeneral ConvertFromSurrogate(in OrleansListQueryResultGeneral surrogate) =>
        surrogate.ToListQueryResultGeneral();

    public OrleansListQueryResultGeneral ConvertToSurrogate(in ListQueryResultGeneral value) =>
        OrleansListQueryResultGeneral.FromListQueryResultGeneral(value);
}
