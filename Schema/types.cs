namespace DataIntegrityTool.Schema;

public enum LoginType
{
	typeUser	 = 1,
	typeCustomer = 2,
	typeAdministrator		 = 3
}

public enum ToolTypes
{ 
	tooltypeVFX,
	tooltypeDI,
	tooltypeArchive,
	tooltypeProduction,
	tooltypeMetaData
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
	errorCustomerSubscriptionNotFound = 16,
	errorNotAuthenticated     = 17,
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

public enum SubscriptionType
{
	subscriptionTrial			= 13,
	subscriptionBronzeMonthly	= 17,
	subscriptionSilverMonthly	= 18,
	subscriptionGoldMonthly		= 19,
	subscriptionBronzeAnnual	= 20,
	subscriptionSilverAnnual	= 21,
	subscriptionGoldAnnual		= 22
}