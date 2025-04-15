using DataIntegrityTool.Schema;

namespace DataIntegrityTool.Shared
{
    public class EncryptionWrapperDIT
    {
        public Int32            primaryKey      { get; set; }
        public CustomerOrUser   type            { get; set; }  
        public byte[]           aesIV           { get; set; } 
        public string           encryptedData   { get; set; }
    }
}

public class RegisterCustomerRequest
{
    public byte[] aesKey        { get; set; }
    public string Name          { get; set; }
    public string Description   { get; set; }
    public string EmailContact  { get; set; }
    public string Notes         { get; set; }
}

