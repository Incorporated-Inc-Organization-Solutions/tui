using System.ComponentModel.DataAnnotations;

namespace tui.Services;

public static class ValidationHelper
{
    public static Dictionary<string, string[]> Validate(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model);
        Validator.TryValidateObject(model, validationContext, validationResults, validateAllProperties: true);

        return validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty(string.Empty)
                .Select(member => new { Member = member, Error = result.ErrorMessage ?? "Ongeldige invoer." }))
            .GroupBy(item => item.Member)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Error).ToArray());
    }
}
