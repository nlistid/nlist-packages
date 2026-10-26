namespace Nlist.Results;

using Nlist.Results.Abstractions;

/// <summary>
/// Chains steps that return <see cref="IResult{T}"/>. On success the function runs on the value;
/// on failure it is skipped and the failure is carried on as the same type with the same data,
/// so the first failure in a chain is the one that comes out of it.
/// </summary>
public static class ResultExtensions
{
    public static IResult<TOut> Bind<T, TOut>(this IResult<T> result, Func<T, IResult<TOut>> func)
    {
        if (result.IsSuccess)
        {
            return func(result.Value!);
        }
        else
        {
            return Propagate<T, TOut>(result);
        }
    }

    public static async Task<IResult<TOut>> BindAsync<T, TOut>(
        this IResult<T> result,
        Func<T, Task<IResult<TOut>>> func)
    {
        if (result.IsSuccess)
        {
            return await func(result.Value!).ConfigureAwait(false);
        }
        else
        {
            return Propagate<T, TOut>(result);
        }
    }

    public static async Task<IResult<TOut>> BindAsync<T, TOut>(
        this Task<IResult<T>> task,
        Func<T, Task<IResult<TOut>>> func)
    {
        var result = await task.ConfigureAwait(false);

        return await result.BindAsync(func).ConfigureAwait(false);
    }

    public static async Task<IResult<TOut>> BindAsync<T, TOut>(
        this Task<IResult<T>> task,
        Func<T, IResult<TOut>> func)
    {
        var result = await task.ConfigureAwait(false);

        return result.Bind(func);
    }

    public static IResult<TOut> Map<T, TOut>(this IResult<T> result, Func<T, TOut> func)
    {
        if (result.IsSuccess)
        {
            return new SuccessResult<TOut>(func(result.Value!));
        }
        else
        {
            return Propagate<T, TOut>(result);
        }
    }

    public static async Task<IResult<TOut>> MapAsync<T, TOut>(
        this IResult<T> result,
        Func<T, Task<TOut>> func)
    {
        if (result.IsSuccess)
        {
            return new SuccessResult<TOut>(await func(result.Value!).ConfigureAwait(false));
        }
        else
        {
            return Propagate<T, TOut>(result);
        }
    }

    public static async Task<IResult<TOut>> MapAsync<T, TOut>(
        this Task<IResult<T>> task,
        Func<T, TOut> func)
    {
        var result = await task.ConfigureAwait(false);

        return result.Map(func);
    }

    public static async Task<IResult<TOut>> MapAsync<T, TOut>(
        this Task<IResult<T>> task,
        Func<T, Task<TOut>> func)
    {
        var result = await task.ConfigureAwait(false);

        return await result.MapAsync(func).ConfigureAwait(false);
    }

    // A failure from outside Nlist.Results has no type to keep, only its code and detail.
    private static IResult<TOut> Propagate<T, TOut>(IResult<T> result)
    {
        if (result is IFailureResult failure)
        {
            return failure.Propagate<TOut>();
        }
        else
        {
            return new StatusCodeFailureResult<TOut>(result.Code, result.Detail);
        }
    }
}
