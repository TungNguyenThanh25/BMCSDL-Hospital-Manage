using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;

namespace HospitalManage.Models
{
    public class HybridOracle
    {
        private readonly OracleConnection conn;


        public HybridOracle(OracleConnection connection)
        {
            conn = connection;
        }

        // 🔐 Mã hóa dữ liệu + khóa DES → trả về Base64
        public string EncryptHybrid(string plainText, string desKey, string rsaPublicKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("HYBRID.encrypt", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Raw, 4000)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(resultParam);

                    cmd.Parameters.Add("p_plainText", OracleDbType.Varchar2).Value = plainText;
                    cmd.Parameters.Add("p_desKey", OracleDbType.Varchar2).Value = desKey;
                    cmd.Parameters.Add("p_rsaPubKey", OracleDbType.Varchar2).Value = rsaPublicKey;

                    cmd.ExecuteNonQuery();

                    if (resultParam.Value != DBNull.Value)
                    {
                        OracleBinary raw = (OracleBinary)resultParam.Value;
                        return Convert.ToBase64String(raw.Value);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi mã hóa HYBRID: " + ex.Message);
                return null;
            }
        }

        // 🔓 Giải mã dữ liệu + khóa DES
        public string DecryptHybrid(string encryptedDataBase64, string encryptedKeyBase64, string rsaPrivateKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("HYBRID.decrypt", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Varchar2, 4000)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(resultParam);

                    byte[] encryptedData = Convert.FromBase64String(encryptedDataBase64);
                    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                    cmd.Parameters.Add("p_encryptedText", OracleDbType.Raw).Value = encryptedData;
                    cmd.Parameters.Add("p_encryptedDesKey", OracleDbType.Raw).Value = encryptedKey;
                    cmd.Parameters.Add("p_rsaPriKey", OracleDbType.Varchar2).Value = rsaPrivateKey;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi giải mã HYBRID: " + ex.Message);
                return "[Giải mã thất bại]";
            }
        }

        // 🔐 Mã hóa riêng khóa DES
        public string EncryptDESKey(string desKey, string rsaPublicKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("HYBRID.encrypt_des_key", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Raw, 4000)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(resultParam);

                    cmd.Parameters.Add("p_desKey", OracleDbType.Varchar2).Value = desKey;
                    cmd.Parameters.Add("p_rsaPubKey", OracleDbType.Varchar2).Value = rsaPublicKey;

                    cmd.ExecuteNonQuery();

                    if (resultParam.Value != DBNull.Value)
                    {
                        OracleBinary raw = (OracleBinary)resultParam.Value;
                        return Convert.ToBase64String(raw.Value);
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi mã hóa khóa DES: " + ex.Message);
                return null;
            }
        }

        // 🔓 Giải mã riêng khóa DES
        public string DecryptDESKey(string encryptedKeyBase64, string rsaPrivateKey)
        {
            try
            {
                using (OracleCommand cmd = new OracleCommand("HYBRID.decrypt_des_key", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter("RETURN_VALUE", OracleDbType.Varchar2, 100)
                    {
                        Direction = ParameterDirection.ReturnValue
                    };
                    cmd.Parameters.Add(resultParam);

                    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
                    cmd.Parameters.Add("p_encryptedDesKey", OracleDbType.Raw).Value = encryptedKey;
                    cmd.Parameters.Add("p_rsaPriKey", OracleDbType.Varchar2).Value = rsaPrivateKey;

                    cmd.ExecuteNonQuery();

                    return resultParam.Value?.ToString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi giải mã khóa DES: " + ex.Message);
                return "[Giải mã khóa thất bại]";
            }

        }
    }
}
