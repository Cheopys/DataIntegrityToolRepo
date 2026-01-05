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
					types.Add(LoginType.typeAdministrator);
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

				if (loginType == LoginType.typeAdministrator)
				{
					Administrators? administrator = context.Administrators.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (administrator != null)
					{
						if (administrator.PasswordHash.Equals(PasswordHash))
						{
							response.PrimaryKey = administrator.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUserId;
					}
				}
				else if (loginType == LoginType.typeCustomer)
				{
					Customers? customer = context.Customers.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (customer != null)
					{
						if (customer.PasswordHash.Equals(PasswordHash))
						{
							response.PrimaryKey = customer.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUserId;
					}
				} // end is customer

				else if (loginType == LoginType.typeUser)
				{
					Users? user = context.Users.Where(us => us.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

					if (user != null)
					{
						if (user.PasswordHash.Equals(PasswordHash))
						{
							response.PrimaryKey = user.Id;
						}
						else
						{
							response.errorcode = ErrorCodes.errorInvalidPassword;
						}
					}
					else
					{
						response.errorcode = ErrorCodes.errorInvalidUserId;
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

		public static SubscriptionRefundResponse AdminRefundSubscription(Int32 CustomerId, 
																		 Int32 SubscriptionId)
		{
			SubscriptionRefundResponse response = new()
			{
				CustomerId		= CustomerId,
				SubscriptionId	= SubscriptionId,
				scansRemaining	= 0,
				ErrorCode		= ErrorCodes.errorNone
			};

			using (DataContext context = new())
			{
				Customers? customer = context.Customers.Find(CustomerId);

				if (customer != null)
				{
					SubscriptionTypes? subscription				 = context.SubscriptionTypes.Find(SubscriptionId);
					CustomerSubscriptions? customerSubscriptions = context.CustomerSubscriptions.Where(cs => cs.CustomerId    .Equals(CustomerId)
					                                                                                      && cs.SubscriptionId.Equals(SubscriptionId))
																							    .LastOrDefault();

					if (customerSubscriptions != null)
					{
						if (customer.Scans >= subscription.scans)
						if (customer.Scans >= subscription.scans)
						{
							customer.Scans -= subscription.scans;
						}
						else
						{
							customer.Scans = 0;
						}

						response.scansRemaining = customer.Scans;

						context.CustomerSubscriptions.Remove(customerSubscriptions);
					}
					else
					{
						response.ErrorCode = ErrorCodes.errorCustomerSubscriptionNotFound;
					}

					context.SaveChangesAsync();
				}
				else
				{
					response.ErrorCode = ErrorCodes.errorInvalidCustomerId;
				}

				context.DisposeAsync();
			}

			return response;
		}

		public static Int32 AdminRefundTopUp(Int32 CustomerId,
											 Int32 scansRefunded)
		{
			Int32 scansRemaining = 0;

			using (DataContext context = new())
			{
				Customers? customer = context.Customers.Find(CustomerId);

				if (customer != null)
				{
					if (customer.Scans >= scansRefunded)
					{
						customer.Scans -= scansRefunded;

						scansRemaining = customer.Scans;
					}
					else
					{
						customer.Scans = 0;
					}

					context.SaveChangesAsync();
				}

				context.DisposeAsync();
			}

			return scansRemaining;
		}
	}
}
