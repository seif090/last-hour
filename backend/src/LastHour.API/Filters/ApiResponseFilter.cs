using LastHour.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LastHour.API.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;
            var value = objectResult.Value;

            object? data = null;
            object? meta = null;
            var message = "Success";
            string[]? errors = null;

            if (statusCode >= 400)
            {
                message = GetErrorFromValue(value) ?? "An error occurred";
                errors = [message];
            }
            else if (value != null)
            {
                var type = value.GetType();

                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();

                    if (genericDef == typeof(Result<>))
                    {
                        var succeeded = (bool)(type.GetProperty("Succeeded")?.GetValue(value) ?? true);
                        if (succeeded)
                        {
                            data = type.GetProperty("Data")?.GetValue(value);
                        }
                        else
                        {
                            message = type.GetProperty("Error")?.GetValue(value)?.ToString() ?? "Error";
                            errors = [message];
                            statusCode = (int)(type.GetProperty("StatusCode")?.GetValue(value) ?? 400);
                        }
                    }
                    else if (genericDef == typeof(PaginatedList<>))
                    {
                        var itemsProp = type.GetProperty("Items");
                        data = itemsProp?.GetValue(value);

                        var total = (int)(type.GetProperty("TotalCount")?.GetValue(value) ?? 0);
                        var page = (int)(type.GetProperty("Page")?.GetValue(value) ?? 1);
                        var pageSize = (int)(type.GetProperty("PageSize")?.GetValue(value) ?? 20);
                        var totalPages = (int)(type.GetProperty("TotalPages")?.GetValue(value) ?? 1);
                        var hasNext = (bool)(type.GetProperty("HasNextPage")?.GetValue(value) ?? false);

                        meta = new
                        {
                            pagination = new
                            {
                                total,
                                page,
                                page_size = pageSize,
                                total_pages = totalPages,
                                has_more = hasNext
                            }
                        };
                    }
                    else
                    {
                        data = value;
                    }
                }
                else if (type == typeof(Result))
                {
                    var succeeded = (bool)(type.GetProperty("Succeeded")?.GetValue(value) ?? true);
                    if (!succeeded)
                    {
                        message = type.GetProperty("Error")?.GetValue(value)?.ToString() ?? "Error";
                        errors = [message];
                        statusCode = (int)(type.GetProperty("StatusCode")?.GetValue(value) ?? 400);
                    }
                }
                else
                {
                    data = value;
                }
            }

            context.Result = new ObjectResult(new
            {
                success = statusCode < 400,
                message,
                data,
                meta,
                errors
            })
            { StatusCode = statusCode };
        }

        await next();
    }

    private static string? GetErrorFromValue(object? value)
    {
        if (value is Result r) return r.Error;
        var errorProp = value?.GetType().GetProperty("Error");
        return errorProp?.GetValue(value)?.ToString();
    }
}
