using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Application
{
    public class NotEmptyGuidAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            // If value is null, it's invalid
            if (value == null)
            {
                return false;
            }

            // Check if the value is a non-empty GUID
            return value is Guid guid && guid != Guid.Empty;
        }
    }
}
