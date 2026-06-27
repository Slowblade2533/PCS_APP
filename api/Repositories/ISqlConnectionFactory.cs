using System.Data;

namespace PCS_API.Repositories;

public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
}
