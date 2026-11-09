using RulesEngine.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Net;
using Volo.Abp;

public class CustomExceptionFilter : IExceptionFilter
{
    private readonly IStringLocalizer<RulesEngineResource> _localizer;

    public CustomExceptionFilter(IStringLocalizer<RulesEngineResource> localizer)
    {
        _localizer = localizer;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is UserFriendlyException userFriendlyException)
        {
            // Localize the message based on the exception code or key
            context.Result = new JsonResult(new
            {
                Code = userFriendlyException.Code,
                Message = _localizer[userFriendlyException.Message].Value
            });
            context.HttpContext.Response.StatusCode = 400;
            context.ExceptionHandled = true;
        }
        else if (context.Exception is ArgumentException abpException)
        {
            context.Result = new JsonResult(new
            {
                Message = abpException.Message
            });
            context.HttpContext.Response.StatusCode = 400;
            context.ExceptionHandled = true;

        }
        else if (context.ModelState != null && !context.ModelState.IsValid)
        {
            // Handle model validation errors
            var errors = context.ModelState
                .Where(ms => ms.Value.Errors.Any())
                .Select(ms => new
                {
                    Field = ms.Key,
                    Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                });

            context.Result = new BadRequestObjectResult(new
            {
                Message = $"{_localizer["ValidationField"]}  <br> " +
                string.Join("; ", errors.Select(e => $"{_localizer["Field"]}: {e.Field},  <br> {_localizer["error"]}: {string.Join(", ", e.Errors)}"))
            });
            context.ExceptionHandled = true;
        }
        else if (context.Exception is DbUpdateException sqlEx && sqlEx.InnerException is Microsoft.Data.SqlClient.SqlException innerSqlEx)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var response = new { Message = "An unexpected error occurred.", Details = string.Empty };

            switch (innerSqlEx.Number)
            {
                case 2627: // Unique constraint violation
                case 2601: // Duplicate key error
                    {
                        // Check for specific field violations
                        if (innerSqlEx.Message.Contains("IX_Financier_Name"))
                        {
                            response = new
                            {
                                Message = _localizer["financier:FinancierEnglishNameIsAlreadyExist"].Value,
                                Details = sqlEx.Message
                            };
                        }
                        else if (innerSqlEx.Message.Contains("IX_Financier_NameAr"))
                        {
                            response = new
                            {
                                Message = _localizer["financier:FinancierArabicNameIsAlreadyExist"].Value,
                                Details = sqlEx.Message
                            };
                        }
                        else if (innerSqlEx.Message.Contains("IX_Financier_TaxRegistrationId"))
                        {
                            response = new
                            {
                                Message = _localizer["financier:TaxRegistrationIdIsAlreadyExist"].Value,
                                Details = sqlEx.Message
                            };
                        }
                        else
                        {
                            response = new
                            {
                                Message = "A unique constraint violation occurred.",
                                Details = sqlEx.Message
                            };
                        }
                        context.Result = new BadRequestObjectResult(new
                        {
                            Message = response.Message,
                            Details = response.Details
                        });

                        statusCode = HttpStatusCode.BadRequest;
                        break;
                    }
            }

            context.HttpContext.Response.StatusCode = (int)statusCode;
            context.ExceptionHandled = true;

        }
        else
        {
            // Handle other exceptions with a generic localized message
            context.Result = new JsonResult(new
            {
                Message = _localizer[context.Exception.Message]
            });
            context.HttpContext.Response.StatusCode = 500;
            context.ExceptionHandled = true;
        }
    }
}
