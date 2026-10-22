using Microsoft.Extensions.Localization;
using RulesEngine.Localization;
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace RulesEngine.Common.Helper
{
    public static class EnumExtensions
    {
        private static IStringLocalizer<RulesEngineResource> _localizer;

        // Method to set the localizer
        public static void SetLocalizer(IStringLocalizer<RulesEngineResource> localizer)
        {
            _localizer = localizer;
        }

        public static string GetDescription(this Enum value)
        {
            FieldInfo field = value.GetType().GetField(value.ToString());
            DescriptionAttribute attribute = field.GetCustomAttribute<DescriptionAttribute>();

            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static string GetLocaliz(this Enum value)
        {

            return _localizer[value.ToString()];
        }
        public static string GetDisplayName(this Enum value)
        {
            if (value == null) return null;
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();
            DisplayAttribute attribute = field.GetCustomAttribute<DisplayAttribute>();
            return attribute?.Name ?? value.ToString();
        }
    }

}
