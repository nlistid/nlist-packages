namespace Nlist.Results;

using Nlist.Results.Abstractions;

/// <summary>
/// Chains steps that return <see cref="IResult{T}"/>. On success the function runs on the value;
/// on failure it is skipped and the failure is carried on as the same type with the same data,
/// so the first failure in a chain is the one that comes out of it. <c>OnSuccess</c> and
/// <c>OnFailure</c> run a side effect, such as logging, and return the same result.
/// </summary>
public static class ResultExtensions
{
    public static IResult<TOut> Bind<T, TOut>(this IResult<T> result, Func<T, IResult<TOut>> func)
    {
        return result.IsSuccess
            ? func(result.Value!)
            : Propagate<T, TOut>(result);
    }

    public static async Task<IResult<TOut>> BindAsync<T, TOut>(
        this IResult<T> result,
        Func<T, Task<IResult<TOut>>> func)
    {
        return result.IsSuccess
            ? await func(result.Value!).ConfigureAwait(false)
            : Propagate<T, TOut>(result);
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
        return result.IsSuccess
            ? new SuccessResult<TOut>(func(result.Value!))
            : Propagate<T, TOut>(result);
    }

    public static async Task<IResult<TOut>> MapAsync<T, TOut>(
        this IResult<T> result,
        Func<T, Task<TOut>> func)
    {
        return result.IsSuccess
            ? new SuccessResult<TOut>(await func(result.Value!).ConfigureAwait(false))
            : Propagate<T, TOut>(result);
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

    public static IResult<T> OnSuccess<T>(this IResult<T> result, Action<T> action)
    {
        if (result.IsSuccess)
        {
            action(result.Value!);
        }

        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(
        this IResult<T> result,
        Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value!).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(
        this Task<IResult<T>> task,
        Action<T> action)
    {
        var result = await task.ConfigureAwait(false);

        return result.OnSuccess(action);
    }

    public static async Task<IResult<T>> OnSuccessAsync<T>(
        this Task<IResult<T>> task,
        Func<T, Task> action)
    {
        var result = await task.ConfigureAwait(false);

        return await result.OnSuccessAsync(action).ConfigureAwait(false);
    }

    public static IResult<T> OnFailure<T>(this IResult<T> result, Action<IResult<T>> action)
    {
        if (!result.IsSuccess)
        {
            action(result);
        }

        return result;
    }

    public static async Task<IResult<T>> OnFailureAsync<T>(
        this IResult<T> result,
        Func<IResult<T>, Task> action)
    {
        if (!result.IsSuccess)
        {
            await action(result).ConfigureAwait(false);
        }

        return result;
    }

    public static async Task<IResult<T>> OnFailureAsync<T>(
        this Task<IResult<T>> task,
        Action<IResult<T>> action)
    {
        var result = await task.ConfigureAwait(false);

        return result.OnFailure(action);
    }

    public static async Task<IResult<T>> OnFailureAsync<T>(
        this Task<IResult<T>> task,
        Func<IResult<T>, Task> action)
    {
        var result = await task.ConfigureAwait(false);

        return await result.OnFailureAsync(action).ConfigureAwait(false);
    }

    // A failure from outside Nlist.Results has no type to keep, only its code and detail.
    private static IResult<TOut> Propagate<T, TOut>(IResult<T> result)
    {
        return result is IFailureResult failure
            ? failure.Propagate<TOut>()
            : new StatusCodeFailureResult<TOut>(result.Code, result.Detail);
    }
}
