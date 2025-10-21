using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace HospitalManage.Models
{
    public class RSAOracle
    {
        OracleConnection conn;
        public RSAOracle(OracleConnection conn)
        {
            this.conn = conn;
        }

        public string Encrypt(string plainText, string pubKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("RSA.encrypt", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Raw, 2000);
                    resultParam.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(resultParam);

                    cmd.Parameters.Add("p_plainText", OracleDbType.Varchar2).Value = plainText;
                    cmd.Parameters.Add("pubKey", OracleDbType.Varchar2).Value = pubKey;

                    cmd.ExecuteNonQuery();

                    if (resultParam.Value != DBNull.Value)
                    {
                        OracleBinary ret = (OracleBinary)resultParam.Value;
                        return Convert.ToBase64String(ret.Value);
                    }
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RSA Encrypt Error: " + ex.Message);
                return null;
            }
        }

        public string Decrypt(string encryptedBase64, string priKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("RSA.decrypt", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Varchar2, 4000);
                    resultParam.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(resultParam);

                    cmd.Parameters.Add("p_encryptedText", OracleDbType.Raw).Value = Convert.FromBase64String(encryptedBase64);
                    cmd.Parameters.Add("priKey", OracleDbType.Varchar2).Value = priKey;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RSA Decrypt Error: " + ex.Message);
                return null;
            }
        }
    }
}
