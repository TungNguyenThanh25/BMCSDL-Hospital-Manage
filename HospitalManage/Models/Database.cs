using Microsoft.Win32.SafeHandles;
using Oracle.ManagedDataAccess.Client;
namespace HospitalManage.Models
{
    public static class Database
    {
        private static readonly string _connectionString;
        public static OracleConnection conn;
        static Database()
        {
            // Đọc chuỗi kết nối từ appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            _connectionString = configuration.GetConnectionString("OracleDBConnection");            
        }
        public static bool IsConnect()
        {
            try
            {
                conn = new OracleConnection( _connectionString);
                conn.Open();
                return true;
            }
            catch (OracleException ex)
            {
                Console.WriteLine($"Oracle error: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return false;
            }
        }

        public static OracleConnection getConnect()
        {
            if (conn == null)
            {
                IsConnect();
            }
            return conn;
        }

    }
}
