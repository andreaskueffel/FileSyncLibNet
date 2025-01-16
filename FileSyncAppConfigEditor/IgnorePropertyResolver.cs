using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Reflection;

namespace FileSyncAppConfigEditor
{
    //short helper class to ignore some properties from serialization
    public class IgnorePropertyResolver: DefaultContractResolver
    {
        private readonly HashSet<string> ignoreProps;
        public IgnorePropertyResolver(params string[] propNamesToIgnore)
        {
            this.ignoreProps = new HashSet<string>(propNamesToIgnore);
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (this.ignoreProps.Contains(property.PropertyName))
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }
}

