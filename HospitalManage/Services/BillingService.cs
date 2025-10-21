using Oracle.ManagedDataAccess.Client;

namespace HospitalManage.Services
{
    public class BillingService
    {
        private readonly string _connectionString;

        public BillingService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("OracleDBConnection");
        }

        // 🔍 Tính tiền thuốc từ bảng prescriptions và medications
        public double CalculateDrugFee(int medicalRecordId)
        {
            double total = 0;
            try
            {
                using (var conn = new OracleConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT SUM(p.quantity * m.price)
                            FROM prescriptions p
                            JOIN medications m ON p.medication_id = m.id
                            WHERE p.medical_record_id = :id";

                        cmd.Parameters.Add(new OracleParameter("id", medicalRecordId));

                        var result = cmd.ExecuteScalar();
                        total = result != null ? Convert.ToDouble(result) : 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("❌ Lỗi truy vấn tiền thuốc: " + ex.Message);
            }

            return total;
        }

        // 💾 Lưu hóa đơn vào bảng BILL với 3 loại mã hóa
        public void SaveBill(BillModel bill, out string message)
        {
            try
            {
                using (var conn = new OracleConnection(_connectionString))
                {
                    conn.Open();

                    // 🔍 Kiểm tra trùng hóa đơn theo hồ sơ và ngày
                    using (var checkCmd = conn.CreateCommand())
                    {
                        checkCmd.CommandText = @"
                            SELECT COUNT(*) FROM BILL
                            WHERE MEDICAL_RECORD_ID = :recordId
                            AND TRUNC(BILL_DATE) = TRUNC(SYSDATE)";

                        checkCmd.Parameters.Add(new OracleParameter("recordId", bill.MedicalRecordId));

                        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                        {
                            message = $"❌ Hóa đơn đã tồn tại cho hồ sơ {bill.MedicalRecordId} trong ngày hôm nay.";
                            return;
                        }
                    }

                    // ✅ Chèn hóa đơn mới
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            INSERT INTO BILL (
                                ID, MEDICAL_RECORD_ID, TOTAL_AMOUNT, BILL_DATE,
                                ENCRYPTED_DRUG_FEE, ENCRYPTED_EXAM_FEE,
                                ENCRYPTED_HYBRID_DATA, ENCRYPTED_HYBRID_KEY,
                                CREATED_AT
                            ) VALUES (
                                bill_seq.NEXTVAL, :recordId, :total, SYSDATE,
                                :drugFee, :examFee, :hybridData, :hybridKey, SYSTIMESTAMP
                            )";

                        cmd.Parameters.Add(new OracleParameter("recordId", bill.MedicalRecordId));
                        cmd.Parameters.Add(new OracleParameter("total", bill.TotalAmount));
                        cmd.Parameters.Add(new OracleParameter("drugFee", bill.EncryptedDrugFee));
                        cmd.Parameters.Add(new OracleParameter("examFee", bill.EncryptedExamFee));
                        cmd.Parameters.Add(new OracleParameter("hybridData", bill.EncryptedData));
                        cmd.Parameters.Add(new OracleParameter("hybridKey", bill.EncryptedKey));

                        cmd.ExecuteNonQuery();
                        message = $"✅ Đã lưu hóa đơn cho hồ sơ {bill.MedicalRecordId}";
                    }
                }
            }
            catch (Exception ex)
            {
                message = $"❌ Lỗi lưu hóa đơn: {ex.Message}";
            }
        }

        // 🔍 Truy xuất hóa đơn theo mã hồ sơ
        public BillModel GetBillByRecordId(int recordId)
        {
            BillModel bill = null;
            try
            {
                using (var conn = new OracleConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT TOTAL_AMOUNT, ENCRYPTED_DRUG_FEE, ENCRYPTED_EXAM_FEE,
                                   ENCRYPTED_HYBRID_DATA, ENCRYPTED_HYBRID_KEY
                            FROM BILL
                            WHERE MEDICAL_RECORD_ID = :id";

                        cmd.Parameters.Add(new OracleParameter("id", recordId));

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                bill = new BillModel
                                {
                                    MedicalRecordId = recordId,
                                    TotalAmount = reader.GetDouble(0),
                                    EncryptedDrugFee = reader.GetString(1),
                                    EncryptedExamFee = reader.GetString(2),
                                    EncryptedData = reader.GetString(3),
                                    EncryptedKey = reader.GetString(4)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("❌ Lỗi truy vấn hóa đơn: " + ex.Message);
            }

            return bill;
        }
    }
}
