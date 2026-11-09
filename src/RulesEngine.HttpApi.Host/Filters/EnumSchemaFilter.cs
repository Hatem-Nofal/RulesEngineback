//using Microsoft.OpenApi.Any;
//using Microsoft.OpenApi.Models;
//using Swashbuckle.AspNetCore.SwaggerGen;
//using System;
//using System.ComponentModel;
//using System.Linq;

//namespace RulesEngineResource.Filters;

//public class EnumSchemaFilter : ISchemaFilter
//{
//    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
//    {
//        if (context.Type.IsEnum)
//        {
//            var enumDescriptions = new OpenApiArray();

//            foreach (var name in Enum.GetNames(context.Type))
//            {
//                var enumMember = context.Type.GetMember(name).FirstOrDefault();
//                var descriptionAttribute = enumMember?.GetCustomAttributes(false)
//                    .OfType<DescriptionAttribute>()
//                    .FirstOrDefault();

//                var description = descriptionAttribute?.Description ?? name;
//                enumDescriptions.Add(new OpenApiString($"{name} = {description}"));
//            }

//            schema.Extensions.Add("x-enumDescriptions", enumDescriptions);
//        }
//    }
//}


