using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Text;

namespace HospitalManage.Models
{
    public class DesOracle
    {
        OracleConnection conn;
        public DesOracle(OracleConnection conn)
        {
            this.conn = conn;
        }
        public string Encrypt(string plainText, string prikey)
        {
            try
            {
                string functionName = "DES.encrypt";

                using (OracleCommand cmd = new OracleCommand(functionName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Giả sử DES.encrypt là FUNCTION trả về RAW (byte array)
                    // Thêm tham số trả về
                    OracleParameter resultParam = new OracleParameter();
                    resultParam.ParameterName = "RETURN_VALUE";
                    resultParam.OracleDbType = OracleDbType.Raw;
                    resultParam.Direction = ParameterDirection.ReturnValue;
                    resultParam.Size = 2000;
                    cmd.Parameters.Add(resultParam);

                    // Tham số input plainText
                    OracleParameter paramPlainText = new OracleParameter();
                    paramPlainText.ParameterName = "p_plainText";
                    paramPlainText.OracleDbType = OracleDbType.Varchar2;
                    paramPlainText.Value = plainText;
                    paramPlainText.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(paramPlainText);

                    // Tham số input key
                    OracleParameter paramKey = new OracleParameter();
                    paramKey.ParameterName = "prikey";
                    paramKey.OracleDbType = OracleDbType.Varchar2;
                    paramKey.Value = prikey;
                    paramKey.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(paramKey);

                    cmd.ExecuteNonQuery();

                    if (resultParam.Value != DBNull.Value)
                    {
                        OracleBinary ret = (OracleBinary)resultParam.Value;
                        // Chuyển mảng byte thành Base64 string để dễ lưu trữ, truyền đi
                        return Convert.ToBase64String(ret.Value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().ToString());
                return null;
            }
        }        

        public string DecryptDES(string encryptedBase64, string prikey)
        {
            try
            {
                string functionName = "DES.decrypt";
                using (OracleCommand cmd = new OracleCommand(functionName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    OracleParameter resultParam = new OracleParameter();
                    resultParam.ParameterName = "RETURN_VALUE";
                    resultParam.OracleDbType = OracleDbType.Varchar2; // Vì hàm trả về VARCHAR2
                    resultParam.Size = 4000; // Kích thước chuỗi lớn nhất
                    resultParam.Direction = ParameterDirection.ReturnValue;
                    cmd.Parameters.Add(resultParam);

                    OracleParameter paramEncrypted = new OracleParameter();
                    paramEncrypted.ParameterName = "p_encryptedText";
                    paramEncrypted.OracleDbType = OracleDbType.Raw;
                    paramEncrypted.Value = Convert.FromBase64String(encryptedBase64);
                    paramEncrypted.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(paramEncrypted);

                    OracleParameter paramKey = new OracleParameter();
                    paramKey.ParameterName = "prikey";
                    paramKey.OracleDbType = OracleDbType.Varchar2;
                    paramKey.Value = prikey;
                    paramKey.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(paramKey);

                    cmd.ExecuteNonQuery();

                    if (resultParam.Value != DBNull.Value)
                    {
                        return resultParam.Value.ToString();
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.GetBaseException().ToString());
                return null;
            }
        }
    }
}
