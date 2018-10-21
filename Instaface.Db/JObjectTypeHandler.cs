using System.Data;
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Instaface.Db
{
    public class JObjectTypeHandler : SqlMapper.TypeHandler<JObject>
    {
        public override JObject Parse(object value)
        {
            return JObject.Parse(value.ToString());
        }

        public override void SetValue(IDbDataParameter parameter, JObject value)
        {
            parameter.Value = value.ToString(Formatting.None);
        }
    }
}

