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
        public LoginType        type            { get; set; }  
        public byte[]           aesIV           { get; set; } 
        public string           encryptedData   { get; set; }
	}

	public class EncryptionWrapperDITString
	{
		public Int32 primaryKey { get; set; }
		public LoginType type { get; set; }
		public string? encryptedData { get; set; }
		public string aesIVString { get; set; }
	}
}

public class RegisterCustomerRequest
{
    public string AesKey        { get; set; }
	public string NameFirst     { get; set; }
	public string NameLast      { get; set; }
    public string Company       { get; set; }
    public string Email         { get; set; }
    public string Password      { get; set; }
    public List<ToolTypes> Tools{ get; set; }
    public string Notes         { get; set; }
	public bool   InitialUser   { get; set; }
	public Int32  SubscriptionId   { get; set; }
}

public class RegisterCustomerResponse 
{
    public Int32      CustomerId { get; set; }
	public ErrorCodes ErrorCode  { get; set; }
}

public class ReprovisionCustomerRequest
{
	public string Email     { get; set; }
	public string Password  { get; set; }
}

public class ReprovisionCustomerResponse
{
	public Int32        CustomerId { get; set; }
	public string       AesKey     { get; set; }
    public ErrorCodes   Error      { get; set; }
}


public class UpdateCustomerRequest
{
    public Int32  Id            { get; set; }
	public string NameFirst     { get; set; }
	public string NameLast      { get; set; }
	public string Email         { get; set; }
	public string Password      { get; set; }
	public List<ToolTypes> Tools{ get; set; }
	public string Notes         { get; set; }
}

public class LoginResponse
{ 
    public Int32      Identifier    { get; set; }
    public ErrorCodes errorcode     { get; set; }
    public Int32      CustOrUserID  { get; set; }
}
public class BeginSessionRequest
{
    public Int32        UserId      { get; set; }
    public LicenseTypes Licensetype { get; set; }
    public ToolTypes    Tooltype    { get; set; }
}

public class  BeginSessionResponse
{
    public Int32 SessionId        { get; set; }
    public DateTime? SubscriptionEnd  { get; set; }
	public Int32 RemainingScans { get; set; }
	public ErrorCodes Error       { get; set; }
}

public class EndSessionResponse
{
	public Int32      SessionId     { get; set; }
	public DateTime   TimeBegin     { get; set; }
	public Int32      FrameOrdinal  { get; set; }
	public Int32      LayerOrdinal  { get; set; }
	public ErrorCodes ErrorCode     { get; set; } = ErrorCodes.errorNone;
}

public class UserLicenseAllocation
{
    public Int32 UserId { get; set; }
    public Int32 UserMeteringCount { get; set; }
    public Int32 UserIntervalSeconds { get; set; }
}
public class CustomerUsage
{
    public Int32 CustomerId         { get; set; }
    public Int32 MeteringCount      { get; set; }
    public DateTime EarliestUse     { get; set; }
}

public class RegisterUserRequest
{
    Int32        Id              { get; set; }
    public Int32 CustomerId      { get; set; }
    public string NameFirst      { get; set; }
	public string NameLast       { get; set; }
	public string Email          { get; set; }
    public string Password       { get; set; }
    public string AesKey         { get; set; }
    public List<ToolTypes> Tools { get; set; }
}

public class RegisterUserResponse
{
    public Int32      UserId    { get; set; }
    public ErrorCodes errorCode { get; set; }
}

public class UpdateUserRequest
{
    public Int32   UserId            { get; set; }
	public string? NameFirst         { get; set; }
	public string? NameLast          { get; set; }
	public string? Email             { get; set; }
	public string? Password          { get; set; }
	public List<ToolTypes>? Tools    { get; set; }
}

public class ChangePasswordRequest
{
	public Int32  UserId      { get; set; }
	public Int32  Token       { get; set; }
	public string PasswordNew { get; set; }
}

public class TopupScansResponse
{
    public Int32 CustomerId { get; set; }
    public Int32 ScansAfter { get; set; }
    public ErrorCodes Error { get; set; }
}

public class AddSubscriptionResponse
{
	public Int32      CustomerId    { get; set; }
    public DateTime   Expiration    { get; set; }
	public Int32      ScansAfter    { get; set; }
	public ErrorCodes Error         { get; set; }
}


public class RecoverAESKeyResponse
{
    public byte[]     AesIVCaller   { get; set; }
	public string     AesKeyRecover { get; set; }
	public ErrorCodes ErrorCode     { get; set; }
}

public class RecoverAESKeyRequest
{
    public EncryptionWrapperDIT wrapperCaller   { get; set; }
    public EncryptionWrapperDIT wrapperRecovery  { get; set; }
}