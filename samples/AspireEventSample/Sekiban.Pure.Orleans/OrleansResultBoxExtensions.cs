using ResultBoxes;

namespace Sekiban.Pure.Orleans;

public static class OrleansResultBoxExtensions
{
    public static OrleansResultBox<TValue> ToOrleansResultBox<TValue>(this ResultBox<TValue> resultBox) where TValue : notnull
    {
        return resultBox.IsSuccess ? new OrleansResultBox<TValue>(null, resultBox.GetValue()) : new OrleansResultBox<TValue>(resultBox.GetException(), default);
    }
}