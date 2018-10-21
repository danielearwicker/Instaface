using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Instaface.Db
{
    public class GraphDataDb : IGraphData
    {
        private readonly IDataConnection _data;
        private readonly ILogger<GraphDataDb> _logger;

        public GraphDataDb(IDataConnection data, ILogger<GraphDataDb> logger)
        {
            _data = data;
            _logger = logger;
        }
        
        public async Task<IReadOnlyCollection<Association>> GetAssociations(IEnumerable<int> ids, string type)
        {            
            using (var db = await _data.Connect())
            {
                var idList = string.Join(",", ids);
                if (string.IsNullOrWhiteSpace(idList))
                {
                    return new Association[0];
                }

                var sql = $@"select id, type, attributes, created, `from`, `to` from association
                         where type=@t and `from` in ({idList})";

                _logger.LogInformation($"{sql} --- @t = {type}", type);

                return (await db.QueryAsync<Association>(sql, new { t = type })).ToList();
            }
        }

        public async Task<IReadOnlyCollection<Entity>> GetEntities(IEnumerable<int> ids)
        {
            using (var db = await _data.Connect())
            {
                var idList = string.Join(",", ids);
                if (string.IsNullOrWhiteSpace(idList))
                {
                    return new Entity[0];
                }

                var sql = $@"select id, type, attributes, created from entity
                         where id in ({idList})";

                _logger.LogInformation(sql);

                return (await db.QueryAsync<Entity>(sql)).ToList();
            }
        }

        public async Task CreateAssociation(int from, int to, string type, JObject attributes)
        {
            using (var db = await _data.Connect())
            {
                var count = await db.ExecuteScalarAsync<int>(
                    @"select count(*) from association
                  where type = @ty and `from` = @f and `to` = @t",
                    new { ty = type, f = from, t = to });

                if (count == 0)
                {
                    await db.ExecuteAsync(
                        @"insert into association (type, created, `from`, `to`, `attributes`)
                      values (@ty, @c, @f, @t, @a)",
                        new
                        {
                            ty = type,
                            c = DateTime.UtcNow,
                            f = from,
                            t = to,
                            a = attributes?.ToString(Formatting.None)
                        });
                }
            }
        }

        public async Task<IReadOnlyCollection<int>> GetRandomEntities(string type, int count)
        {
            using (var db = await _data.Connect())
            {
                var available = await db.ExecuteScalarAsync<int>(
                    @"select count(*) from entity where type = @ty",
                    new { ty = type });

                count = Math.Min(available, count);
                var start = new Random().Next(0, available - count);

                return (await db.QueryAsync<int>(
                    $"select id from entity where type = @ty limit {start}, {count}",
                    new { ty = type })).ToList();
            }
        }

        public async Task<int> CreateEntity(string type, JObject attributes)
        {
            using (var db = await _data.Connect())
            {
                return await db.ExecuteScalarAsync<int>(
                    @"insert into entity (`type`, `created`, `attributes`)
                  values (@t, @c, @a);
                  select LAST_INSERT_ID();",
                    new
                    {
                        t = type,
                        c = DateTime.UtcNow,
                        a = attributes?.ToString(Formatting.None)
                    });
            }
        }

        public async Task Init()
        {
            using (var db = await _data.Connect())
            {
                if (db.ExecuteScalar<int>("select count(*) from entity") == 0)
                {
                    await CreateEntity("user", new JObject
                    {
                        ["firstName"] = "Daniel",
                        ["lastName"] = "Earwicker"
                    });
                }
            }
        }
        
        static GraphDataDb()
        {
            SqlMapper.AddTypeHandler(typeof(JObject), new JObjectTypeHandler());
        }
    }
}