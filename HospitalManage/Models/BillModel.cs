public class BillModel
{
    public int MedicalRecordId { get; set; }
    public double DrugFee { get; set; }
    public double ExamFee { get; set; }
    public double TotalAmount { get; set; }

    public string EncryptedDrugFee { get; set; }
    public string EncryptedExamFee { get; set; }
    public string EncryptedData { get; set; }
    public string EncryptedKey { get; set; }
}
