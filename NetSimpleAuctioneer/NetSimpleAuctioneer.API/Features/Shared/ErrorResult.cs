using System.ComponentModel.DataAnnotations;

namespace NetSimpleAuctioneer.API.Features.Shared
{
    public class ErrorResult<T> where T : Enum
    {
        public ErrorResult() { }

        public ErrorResult(T code, params string[] invalidValues)
        {
            Code = code;
            InvalidValues.AddRange(invalidValues);
        }

        public ErrorResult(T code, IEnumerable<string> invalidValues)
        {
            Code = code;
            InvalidValues.AddRange(invalidValues);
        }

        /// <summary>
        /// Error code
        /// </summary>
        [Required]
        public T Code { get; set; } = default!;

        /// <summary>
        /// List of invalid values associated to the error, if any
        /// </summary>
        [Required]
        public List<string> InvalidValues { get; set; } = [];
    }
}
