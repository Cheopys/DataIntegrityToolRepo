namespace DataIntegrityTool.Schema;

public enum CustomerOrUser
{
	typeDIT       = 0,
	typeCustomer  = 1,
	typeUser      = 2
}
public enum LicenseTypes
{
	licenseTypeMetered,
	licenseTypeInterval,
}

public enum ToolTypes
{ 
	tooltypeVFX,
	tooltypeDI,
	tooltypeArchive,
	tooltypeProduction
}

public enum ErrorCodes
{
	errorNone				= 0,
	errorInvalidUser		= 1,
	errorInvalidPassword	= 2,
	errorNoRegistrations    = 3, 
	errorTokenNotFound      = 4,	
	errorToolNotAuthorized  = 5,
	errorNoLicense			= 6,                                                                             
}