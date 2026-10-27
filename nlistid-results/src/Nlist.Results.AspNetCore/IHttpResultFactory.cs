namespace Nlist.Results.AspNetCore;

using Microsoft.AspNetCore.Http;
using Nlist.Results.Abstractions;

public interface IHttpResultFactory
{
    IResult ToHttpResult<T>(IResult<T> result);
}
