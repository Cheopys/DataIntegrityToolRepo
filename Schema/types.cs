namespace DataIntegrityTool.Schema;

public enum CustomerOrUser
{
	typeUndefined = 0,
	typeDIT       = 1,
	typeCustomer  = 2,
	typeUser      = 3
}
public enum LicenseTypes
{
	licenseTypeMetered,
	licenseTypeInterval,
	licenseTypeSubscription
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