using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;

namespace DataIntegrityTool.Services
{
	public class ApplicationService
	{
		public static LoginResponse WebLogin(string		Email,
										     string		PasswordHash,
										     LoginType	loginType)
		{
			LoginResponse response = new()
			{
				errorcode = ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				// From web site

				if (loginType == LoginType.typeDIT)
				{
					Administrators? administrator = context.Administrators.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (administrator != null)
					{
						if (administrator.PasswordHash.Equals(PasswordHash))
						{
							response.Identifier = administrator.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUser;
					}
				}
				else if (loginType == LoginType.typeCustomer)
				{
					Customers? customer = context.Customers.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (customer != null)
					{
						if (customer.PasswordHash.Equals(PasswordHash))
						{
							response.Identifier = customer.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUser;
					}
				} // end is customer

				// from DIT Tool

				else if (loginType == LoginType.typeUser)
				{
					Users? user = context.Users.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (user != null)
					{
						if (user.PasswordHash.Equals(PasswordHash))
						{
							response.Identifier = user.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUser;
					}
				}
				else
				{
					response.errorcode = ErrorCodes.errorUnknownLoginType;
				}
				context.Dispose();
			}

			if (response.errorcode == ErrorCodes.errorNone)
			{
				Program.loginType  = loginType;
				response.loginType = loginType;
			}

			return response;
		}
	}
}
