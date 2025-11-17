namespace DataIntegrityTool.Schema;

public enum LoginType
{
	typeUser	 = 1,
	typeCustomer = 2,
	typeAdministrator		 = 3
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
	errorInvalidUserId		  = 1,
	errorInvalidPassword	  = 2,
	errorNoRegistrations      = 3, 
	errorWrongToken			  = 4,	
	errorToolNotAuthorized    = 5,
	errorNoLicense			  = 6,
	errorBadKeySize           = 7,
	errorUnknownLoginType     = 8,
	errorNoSeats			  = 9,
	errorInvalidCustomerId	  = 10,
	errorInvalidAdministratorId = 11,
	errorInvalidLoginType     = 12,
	errorEmailAlreadyExists   = 13,
	errorAlreadySubscribed	  = 14,
	errorInvalidEmailFormat	  = 15,
}

public enum OSType
{
	Windows = 1,
	Mac		= 2,
	Linux	= 3
}

public enum InterfaceType
{
	GUI = 1,
	CLI	= 2
}