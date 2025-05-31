namespace DataIntegrityTool.Schema;

public enum CustomerOrUser
{
	typeUndefined = 0,
	typeDIT       = 1,
	typeCustomer  = 2,
	typeUser      = 3
}

[Flags]
public enum LicenseTypes
{
	licenseTypeMetered      = 1,
	licenseTypeInterval     = 2,
	licenseTypeSubscription = 4
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
	errorWrongToken			= 4,	
	errorToolNotAuthorized  = 5,
	errorNoLicense			= 6,
	errorBadKeySize         = 7,
}