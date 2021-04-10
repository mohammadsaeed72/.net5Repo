using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace BlazorApp1.Server
{
    public class CustomSqlAdoConfigProvider : ConfigurationProvider
    {
        private string _connectionString;
        public CustomSqlAdoConfigProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public override void Load()
        {
            Data = new Dictionary<string, string>();
            SqlConnection connection=null;
            try
            {
                connection = new SqlConnection(_connectionString);

                SqlCommand com = new SqlCommand("Select * from tblConfig", connection);

                if (com.Connection.State != ConnectionState.Open)
                    com.Connection.Open();
                using (var dr = com.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        Data.Add(dr["Key"].ToString(), dr["value"].ToString());
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                connection?.Dispose();
            }

        }
    }

    public static class ConfigurationBuilderExtensions
    {
        public static IConfigurationBuilder AddCustomConfiguration(this IConfigurationBuilder builder, string connectionString)
        {
            return builder.Add(new CustomSqlAdoConfigSource(connectionString));
        }

    }

    public class CustomSqlAdoConfigSource : IConfigurationSource
    {
        private readonly string _connectionString;

        public CustomSqlAdoConfigSource(
            string connectionString) =>
            _connectionString = connectionString;

        public IConfigurationProvider Build(IConfigurationBuilder builder) =>
            new CustomSqlAdoConfigProvider(_connectionString);
    }
}
