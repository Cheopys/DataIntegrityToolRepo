namespace DataIntegrityTool.Schema;

public enum LoginType
{
	typeUser	 = 1,
	typeCustomer = 2,
	typeDIT		 = 3
}

public enum LicenseTypes
{
	licenseTypeMetered      = 1,
	licenseTypeSubscription = 2
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
	errorNone				  = 0,
	errorInvalidUser		  = 1,
	errorInvalidPassword	  = 2,
	errorNoRegistrations      = 3, 
	errorWrongToken			  = 4,	
	errorToolNotAuthorized    = 5,
	errorNoLicense			  = 6,
	errorBadKeySize           = 7,
	errorUnknownLoginType     = 8,
}