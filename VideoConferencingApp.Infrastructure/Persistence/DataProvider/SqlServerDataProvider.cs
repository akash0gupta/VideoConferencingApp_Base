using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using System.Data.Common;
//using static LinqToDB.DataProvider.SqlServer.SqlServerProviderAdapter;

namespace VideoConferencingApp.Infrastructure.Persistence.DataProvider
{
    public class SqlServerDataProvider : BaseDataProvider
    {
        protected override DbConnection GetInternalDbConnection(string connectionString)
        {
            return new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        }

        protected override IDataProvider LinqToDbDataProvider => SqlServerTools.GetDataProvider(SqlServerVersion.v2017);
    }
}