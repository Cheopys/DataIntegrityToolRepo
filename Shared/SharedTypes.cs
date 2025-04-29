using Amazon.S3.Model;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Shared;


namespace DataIntegrityTool.Shared
{
    public enum Errors
    {
        ErrorNone           = 0,
        licenseNotAvailable = 1,
    }

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
    public List<ToolTypes> Tools{ get; set; }
    public string Notes         { get; set; }
}

public class RegisterUserRequest
{
    public Int32 CompanyId          { get; set; }
    public string Name              { get; set; }
    public string Email             { get; set; }
    public string PasswordHash      { get; set; }
    public string? MFA              { get; set; }
    public List<ToolTypes> Tools    { get; set; }
    public byte[] aeskey            { get; set; }
}

public class BeginSessionRequest
{
    public Int32        UserId      { get; set; }
    public LicenseTypes Licensetype { get; set; }
    public ToolTypes    Tooltype    { get; set; }
}

public class  BeginSessionResponse
{
    public Int32 SessionId          { get; set; }
    public Int32 RemainingSeconds   { get; set; }
    public ErrorCodes Error         { get; set; }
}

public class UserLicenseAllocation
{
    public Int32 UserId { get; set; }
    public Int32 UserMeteringCount { get; set; }
    public Int32 UserIntervalSeconds { get; set; }
}
public class AllocateLicensesRequest
{
    public Int32 CustomerId         { get; set; }
    public bool UserLicensingPool   { get; set; }
    public Int32 MeteringCount      { get; set; }
    public Int32 IntervalSeconds    { get; set; }
    public List<UserLicenseAllocation>? userLicenseAllocations { get; set; }
}
public class AllocateLicensesResponse
{
    public Int32 CustomerId { get; set; }
    public Int32 MeteringCount { get; set; }
    public Int32 IntervalSeconds { get; set; }
}