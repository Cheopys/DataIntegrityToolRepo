using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Identity;
using DataIntegrityTool.Shared;

namespace DataIntegrityTool.Services
{
	public class ApplicationService
	{
		public static List<LoginType> LoginRolesForEmail(string Email)
		{
			List<LoginType> types = new List<LoginType>();
			string EmailLower = Email.ToLower();

			using (DataContext context  = new())
			{
				Administrators? admin =  context.Administrators.Where(ad => ad.Email.ToLower().Equals(EmailLower)).FirstOrDefault();

				if (admin != null)
				{
					types.Add(LoginType.typeDIT);
				}

				Customers? customer = context.Customers.Where(cu => cu.Email.ToLower().Equals(EmailLower)).FirstOrDefault();

				if (customer != null)
				{
					types.Add(LoginType.typeCustomer);
				}

				Users? user = context.Users.Where(us => us.Email.ToLower().Equals(EmailLower)).FirstOrDefault();

				if (user != null)
				{
					types.Add(LoginType.typeUser);
				}

				context.Dispose();
			}

			return types;
		}

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

			return response;
		}
	}
}
