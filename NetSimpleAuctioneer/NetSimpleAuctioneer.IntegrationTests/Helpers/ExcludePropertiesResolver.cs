using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace NetSimpleAuctioneer.IntegrationTests.Helpers
{
    /// <summary>
    /// A custom contract resolver that extends DefaultContractResolver and allows excluding specific properties from serialization.
    /// </summary>
    public class ExcludePropertiesResolver(params string[] propNamesToExclude) : DefaultContractResolver
    {
        private readonly HashSet<string> _excludeProps = new(propNamesToExclude);

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            // Only serialize the property if it's not in the list of properties to include
            property.ShouldSerialize = _ => !_excludeProps.Contains(property.PropertyName!);

            return property;
        }
    }
}
