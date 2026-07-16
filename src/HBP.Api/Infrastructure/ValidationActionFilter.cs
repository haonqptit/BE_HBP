using FluentValidation;
using HBP.Application.Common;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HBP.Api.Infrastructure;

public sealed class ValidationActionFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values.Where(x => x is not null))
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(argument!.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;
            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                var errors = result.Errors.GroupBy(x => x.PropertyName)
                    .ToDictionary(x => x.Key, x => x.Select(e => e.ErrorMessage).ToArray());
                throw new HBP.Application.Common.ValidationException("One or more validation errors occurred.", errors);
            }
        }
        await next();
    }
}
