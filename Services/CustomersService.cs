using Amazon.Runtime.Internal;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DataIntegrityTool.Db;
using DataIntegrityTool.Migrations;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Humanizer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using NLog;
using NLog.LayoutRenderers;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using NuGet.Packaging;
using NuGet.Versioning;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DataIntegrityTool.Services
{
    public static class CustomersService
    {
        static Logger logger;
        static CustomersService()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

            // Apply config           
            NLog.LogManager.Configuration = config;
            logger = LogManager.GetCurrentClassLogger();
        }

        public static RegisterCustomerResponse RegisterCustomer(RegisterCustomerRequest request)
        {
            request.Tools = new List<ToolTypes>()
            {
                0
            };

			RegisterCustomerResponse response = new()
            {
                ErrorCode = ErrorCodes.errorNone
            };

            if (CustomersService.IsValidEmail(request.Email))
            {
                using (DataContext context = new())
                {
                    if (context.Customers.Where(cu => cu.Email.ToLower().Equals(request.Email.ToLower())).FirstOrDefault() == null)
                    {
                        //SubscriptionTypes type = context.SubscriptionTypes.Where(st => st.Id.Equals(13)).FirstOrDefault();
                        Users user = null;
                        Customers customer = new Customers()
                        {
                            NameFirst        = request.NameFirst,
                            NameLast         = request.NameLast,
                            Company          = request.Company,
                            Email            = request.Email,
//                            PhoneNumber      = request.PhoneNumber == null ? "0" : request.PhoneNumber,
                            PasswordHash     = ServerCryptographyService.SHA256(request.Password),
                            Notes            = request.Notes,
                            AesKey           = Convert.FromHexString(request.AesKey),
                            DateAdded        = DateTime.UtcNow,
                            UsageSince       = DateTime.MinValue,
                            Tools            = request.Tools,
                            SeatsMax         = 10,
                            Scans            = 0, //type.scans,
                            SubscriptionTime = null//TimeSpan.FromDays(type.days)
                        };

                        context.Customers.Add(customer);

                        // need the new customer PK to continue

                        context.SaveChanges();

                        response.CustomerId = customer.Id;

                        AddSubscription(customer.Id, 13);

                        if (request.InitialUser)
                        {
                            user = new Users()
                            {
                                AesKey          = Convert.FromHexString(request.AesKey),
                                CustomerId      = customer.Id,
                                Email           = request.Email,
                                NameFirst       = request.NameFirst,
                                NameLast        = request.NameLast,
//                                PhoneNumber     = customer.PhoneNumber,
                                PasswordHash    = ServerCryptographyService.SHA256(request.Password),
                                DateAdded       = DateTime.UtcNow,
                            };

                            context.Users.Add(user);
                        }

                        context.SaveChanges();
                    } // endif email is new
                    else
                    {
                        response.ErrorCode = ErrorCodes.errorEmailAlreadyExists;
                    }

                    context.Dispose();
                }
            }
            else
            {
                response.ErrorCode = ErrorCodes.errorInvalidEmailFormat;
            }

            return response;
        }

        public static ReprovisionCustomerResponse ReprovisionCustomer(ReprovisionCustomerRequest request)
        {
            ReprovisionCustomerResponse response = new()
            {
                Error = ErrorCodes.errorNone
            };

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Email.ToLower().Equals(request.Email.ToLower())).FirstOrDefault();

                if (customer != null)
                {
                    if (customer.PasswordHash.Equals(ServerCryptographyService.SHA256(request.Password)))
                    {
                        response.CustomerId = customer.Id;
                        response.AesKey = Convert.ToHexString(customer.AesKey);
                    }
                    else
                    {
                        response.Error = ErrorCodes.errorInvalidPassword;
                    }
                }
                else
                {
                    response.Error = ErrorCodes.errorInvalidUserId;
                }

                context.Dispose();
            }

            return response;
        }

        public static Customers GetCustomer(Int32 CustomerId)
        {
            Customers? customer = null;

            using (DataContext context = new())
            {
                customer = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();

                context.Dispose();
            }

            return customer;
        }

        public static ErrorCodes UpdateCustomer(UpdateCustomerRequest request)
        {
            ErrorCodes error = ErrorCodes.errorNone;

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(request.CustomerId)).FirstOrDefault();

                if (customer != null)
                {
                    if (request.NameFirst != null)
                    {
                        customer.NameFirst = request.NameFirst;
                    }

                    if (request.NameLast != null)
                    {
                        customer.NameLast = request.NameLast;
                    }

                    if (request.Email != null)
                    {
                        if (CustomersService.IsValidEmail(request.Email))
                        {
                            customer.Email = request.Email;
                        }
                        else
                        {
                            error = ErrorCodes.errorInvalidEmailFormat;
                        }
                    }
                    /*
					if (request.PhoneNumber != null)
					{
						customer.PhoneNumber = request.PhoneNumber;
					}*/

					if (request.Password != null)
                    {
                        customer.PasswordHash = ServerCryptographyService.SHA256(request.Password);
                    }

                    if (request.Notes != null)
                    {
                        customer.Notes = request.Notes;
                    }

                    context.SaveChanges();
				}
                else
                {
                    error = ErrorCodes.errorInvalidCustomerId;
                }

                context.Dispose();
            }

            return error;
        }

        public static void DeleteCustomer(Int32 CustomerId)
        {
            using (DataContext context = new())
            {
                Customers?                  customer        = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();
                List<Users>                 users           = context.Users.Where(us => us.CustomerId.Equals(CustomerId)).ToList();
                List<Session>               sessions        = context.Session.Where(s => s.CustomerId.Equals(CustomerId)).ToList();
                List<LicenseMetered>        licensesetered  = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(CustomerId)).ToList();
                List<CustomerSubscriptions> subscriptions   = context.CustomerSubscriptions.Where(s => s.CustomerId == CustomerId).ToList();

                List<Int32> sessionIds = sessions.Select(s => s.Id).ToList();
                List<SessionTransition> transitions = context.SessionTransition.Where(s => sessionIds.Contains(s.SessionId)).ToList();

                context.RemoveRange(users);
                context.RemoveRange(sessions);
				context.RemoveRange(transitions);
				context.RemoveRange(licensesetered);
				context.RemoveRange(subscriptions);
				context.Remove     (customer);

				context.SaveChanges();
                context.Dispose();
            }
        }

        public static async Task<List<Customers>> GetCustomers()
        {
            List<Customers> customers = null;

            using (DataContext context = new())
            {
                customers = context.Customers.OrderBy(c => c.NameLast).ToList();

                await context.DisposeAsync();
            }

            return customers;
        }

        public static Int32 AddCustomerScans(Int32 CustomerId, 
                                             Int32 newScans)
        {
            Int32 scans = 0;

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();

                customer.Scans += newScans;

                scans = customer.Scans;

                context.SaveChanges();
                context.Dispose();
            }

            return scans;
        }

        private static CustomerUsage UsageByCustomer(Int32 customerId,
                                              DataContext context)
        {
            DateTime earliest = DateTime.MaxValue;

            CustomerUsage usage = new()
            {
                CustomerId = customerId,
            };

            // last time customer was billed
            DateTime? customerUsage = context.Customers.Where(cu => cu.Id.Equals(customerId))
                                                       .Select(cu => cu.UsageSince)
                                                       .FirstOrDefault();
            // metered licenses

            List<LicenseMetered> metereds = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(customerId)
                                                                            && lm.TimeBegun > customerUsage)
                                                                  .ToList();
            usage.ScanCount = metereds.Count();

            if (usage.ScanCount > 0)
            {
                earliest = metereds.Min(lm => lm.TimeBegun.Value);
            }
            else
            {
                earliest = DateTime.MinValue;
            }
            return usage;
        }

        public static List<CustomerUsage> GetCustomerUsages(Int32? customerId)
        {
            List<CustomerUsage> usages = new();

            using (DataContext context = new())
            {
                DateTime lastUsage = context.ToolParameters.Select(tp => tp.usageSince).FirstOrDefault();

                if (customerId != null)
                {
                    usages.Add(UsageByCustomer(customerId.Value, context));
                }
                else
                {
                    List<Int32> customerIds = context.Customers.Select(cu => cu.Id).ToList();

                    foreach (Int32 id in customerIds)
                    {
                        usages.Add(UsageByCustomer(id, context));
                    }
                }

                context.Dispose();
            }

            return usages;
        }

        public static LoginType CheckEmail(string Email)
        {
            LoginType type = LoginType.typeUser;

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

                if (customer != null)
                {
                    type = (customer.Id == 4) ? LoginType.typeAdministrator : LoginType.typeCustomer;
                }
                else
                {
                    Users? user = context.Users.Where(cu => cu.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

                    if (user != null)
                    {
                        type = LoginType.typeUser;
                    }
                }

                context.Dispose();
            }

            return type;
        }

        public static TopupScansResponse TopUpScans(Int32 CustomerId,
                                                    Int16 Count,
                                                    Int32 Amount)
        {
            TopupScansResponse response = new()
            {
                CustomerId = CustomerId,
                Error = ErrorCodes.errorNone
            };

            using (DataContext context = new())
            {
                Customers customer = context.Customers.Find(CustomerId);

                if (customer != null)
                {
                    customer.Scans += Count;

                    context.SaveChanges();

                    response.ScansAfter = customer.Scans;

					context.Add(new CustomerPayments()
					{
						CustomerId       = customer.Id,
						Amount           = Amount,
						Date             = DateTime.UtcNow,
						SubscriptionType = null,
						Scans            = Count
					});

					context.SaveChanges();

				}
				else
                {
                    response.Error = ErrorCodes.errorInvalidCustomerId;
                }

                context.Dispose();
            }

            return response;
        }

        public static AddSubscriptionResponse AddSubscription(Int32 CustomerId,
															  Int32 subscriptionId)
        {
            AddSubscriptionResponse response = new()
            {
                CustomerId = CustomerId,
                Error = ErrorCodes.errorNone
            };

            using (DataContext context = new())
            {
                Customers?             customer     = context.Customers            .Where(cu => cu.Id        .Equals(CustomerId))    .FirstOrDefault();
                SubscriptionTypes?     subscription = context.SubscriptionTypes    .Where(st => st.Id        .Equals(subscriptionId)).FirstOrDefault();
                CustomerSubscriptions? custsub      = context.CustomerSubscriptions.Where(cs => cs.CustomerId.Equals(CustomerId))    .FirstOrDefault();

                if (customer != null)
                {
                    if (custsub == null)
                    {
                        custsub = new CustomerSubscriptions()
                        {
                            CustomerId = CustomerId,
                            SubscriptionId = subscriptionId,
                        };

						context.CustomerSubscriptions .Add(custsub);
                    }
                    // ExpirationDate is null for a subscription that hasn't been used yet

                    if (custsub.ExpirationDate == null
                    && customer.SubscriptionTime != null)
                    {
                        customer.SubscriptionTime += TimeSpan.FromDays(subscription.days);
                    }
                    else
                    {
                        custsub.ExpirationDate = DateTime.UtcNow + (TimeSpan.FromDays(subscription.days) + customer.SubscriptionTime);
                        customer.SubscriptionTime = null;
                    }

					customer.Scans += subscription.scans;

                    response.Expiration = custsub.ExpirationDate;
                    response.ScansAfter = customer.Scans;

                    context.Add(new CustomerPayments()
                    {
                        CustomerId       = customer.Id,
                        Amount           = subscription.price,
                        Date             = DateTime.UtcNow,
                        SubscriptionType = subscriptionId,
                        Scans            = subscription.scans
                    });

					context.SaveChanges();
				}
				else
                {
                    response.Error = ErrorCodes.errorInvalidCustomerId;
                }

                context.Dispose();
            }

            return response;
        }

		public static bool IsValidEmail(string email)
		{
			if (string.IsNullOrWhiteSpace(email))
				return false;

			try
			{
				// Normalize the domain
				email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
									  RegexOptions.None, TimeSpan.FromMilliseconds(200));

				// Examines the domain part of the email and normalizes it.
				string DomainMapper(Match match)
				{
					// Use IdnMapping class to convert Unicode domain names.
					var idn = new IdnMapping();

					// Pull out and process domain name (throws ArgumentException on invalid)
					string domainName = idn.GetAscii(match.Groups[2].Value);

					return match.Groups[1].Value + domainName;
				}
			}
			catch (RegexMatchTimeoutException e)
			{
				return false;
			}
			catch (ArgumentException e)
			{
				return false;
			}

			try
			{
				return Regex.IsMatch(email,
					@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
					RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
			}
			catch (RegexMatchTimeoutException)
			{
				return false;
			}
		}

        public static Int32 CustomerRemainingScans(Int32 customerId)
        {
            Int32 scans = 0;

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Find(customerId);

                if (customer != null)
                {
                    scans = customer.Scans;
                }

                context.Dispose();
            }

                return scans;
        }
	}
}

