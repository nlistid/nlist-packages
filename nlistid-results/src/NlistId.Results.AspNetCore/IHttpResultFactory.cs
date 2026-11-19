namespace NlistId.Results.AspNetCore;

using Microsoft.AspNetCore.Http;
using NlistId.Results.Abstractions;

public interface IHttpResultFactory
{
    IResult ToHttpResult<T>(IResult<T> result);
}
