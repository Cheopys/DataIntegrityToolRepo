using Amazon.Runtime.Internal;
using CloudinaryDotNet.Actions;
using DataIntegrityTool.Db;
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
            RegisterCustomerResponse response = new()
            {
                ErrorCode = ErrorCodes.errorNone
            };

            if (request.AesKey.Length != 32)
			{
			    Customers customer = new Customers()
			    {
				    Name        = request.Name,
				    Description = request.Description,
				    Email       = request.Email,
				    PasswordHash = ServerCryptographyService.SHA256(request.Password),
				    Notes        = request.Notes,
				    AesKey       = Convert.FromHexString(request.AesKey),
				    DateAdded    = DateTime.UtcNow,
				    UsageSince   = DateTime.MinValue,
			    };

			    Users user = new Users()
			    {
				    AesKey                   = Convert.FromHexString(request.AesKey),
				    Email                    = request.Email,
				    Name                     = request.Name,
				    PasswordHash             = ServerCryptographyService.SHA256(request.Password),
				    DateAdded                = DateTime.UtcNow,
				    LicensingIntervalSeconds = 0,
				    LicensingMeteredCount    = 0,
				    Tools                    = request.Tools,
			    };

			    using (DataContext context = new())
			    {
				    context.Customers.Add(customer);

				    context.SaveChanges();

				    user.CustomerId = customer.Id;

                    response.CustomerId = (Int64) customer.Id;

					context.Users.Add(user);

				    context.SaveChanges();
				    context.Dispose();
			    }
			}
            else
            {
                response.ErrorCode = ErrorCodes.errorBadKeySize;
            }

            return response;
		}

            public static Customers GetCustomer (Int32 CustomerId)
            {
			    Customers? customer = null;

			    using (DataContext context = new())
                {
                    customer = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();

                    context.Dispose();
                }

    			return customer;
        }

		public static void UpdateCustomer(UpdateCustomerRequest request)
		{
			using (DataContext context = new())
			{
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(request.Id)).FirstOrDefault();

                if (request.Name != null)
                {
					customer.Name = request.Name;
				}

                if (request.Description != null)
                {
                    customer.Description = request.Description;
                }

                if (request.Email != null)
                {
                    customer.Email = request.Email;
                }

                if (request.Password != null)
                {
                    customer.PasswordHash = ServerCryptographyService.SHA256(request.Password);
                }

                if (request.Notes != null)
                {
                    customer.Notes = request.Notes;
                }

                context.SaveChanges();
				context.Dispose();
			}
		}

		public static void DeleteCustomer(Int32 CustomerId)
        {
            using (DataContext context = new())
            {
                Customers?   customer  = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();
                List<Users>   users    = context.Users    .Where(us => us.CustomerId.Equals(CustomerId)).ToList();
                List<Session> sessions = context.Session  .Where(s  => s .CustomerId.Equals(CustomerId)).ToList();
                List<LicenseInterval> licensesInterval = context.LicenseInterval.Where(li => li.CustomerId.Equals(CustomerId)).ToList();
                List<LicenseMetered>  licensesetered   = context.LicenseMetered .Where(lm => lm.CustomerId.Equals(CustomerId)).ToList();

                context.Remove     (customer);
                context.RemoveRange(users);
                context.RemoveRange(sessions);
                context.RemoveRange(licensesetered);
                context.RemoveRange(licensesInterval);

                context.SaveChanges();
                context.Dispose();
            }
        }

        public static async Task<List<Customers>> GetCustomers()
        {
            List<Customers> customers = null;

            using (DataContext context = new())
            {
                customers = context.Customers.OrderBy(c => c.Name).ToList();

                await context.DisposeAsync();
            }

            return customers;
        }

        public static AllocateLicensesResponse AllocateLicenses(AllocateLicensesRequest request)
        {
            AllocateLicensesResponse response = new()
            {
                UserId = request.UserId
            };

            using (DataContext context = new())
            {
                Users? user = context.Users.Find(request.UserId);

                user.LicensingMeteredCount    += request.MeteringCount;

                response.MeteringCount   = user.LicensingMeteredCount;

                context.SaveChanges();
                context.Dispose();
            }

            return response;
        }

        private static CustomerUsage UsageByCustomer(Int32 customerId, 
                                              DataContext  context)
        {
            DateTime earliest = DateTime.MaxValue;

            CustomerUsage usage = new()
            {
                CustomerId = customerId,
            };

            // last time customer was billed
            DateTime? customerUsage = context.Customers.Where (cu => cu.Id.Equals(customerId))
                                                       .Select(cu => cu.UsageSince)
                                                       .FirstOrDefault();
            // metered licenses

            List<LicenseMetered> metereds = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(customerId)
                                                                            && lm.TimeBegun > customerUsage)
                                                                  .ToList();
            usage.MeteringCount = metereds.Count();

            earliest = metereds.Min(lm => lm.TimeBegun.Value);

            // time interval licenses

            List<LicenseInterval> intervals = context.LicenseInterval.Where(li => li.CustomerId.Equals(customerId)
                                                                               && li.TimeBegin       > customerUsage)
                                                                     .ToList();

//          usage.IntervalSessions = intervals.Count();
            
            foreach (LicenseInterval interval in intervals)
            {
                if (interval.TimeBegin != null
                && interval .TimeEnd   != null)
                {
                    DateTime timeBegin = interval.TimeBegin.Value;
                    DateTime timeEnd   = interval.TimeEnd;
//                  usage.IntervalSeconds += (Int32)(timeEnd.Subtract(timeBegin)).TotalSeconds;
                }
            }

            return usage;
        }

        public static List<CustomerUsage> GetCustomerUsages(Int32? customerId)
        {
            List<CustomerUsage>usages = new ();

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

                    foreach(Int32 id in  customerIds)
                    {
                        usages.Add(UsageByCustomer(id, context));
                    }
                }

                context.Dispose();
            }

            return usages;
        }

        public static CustomerOrUser CheckEmail(string Email)
        {
			CustomerOrUser type = CustomerOrUser.typeUser;
			
            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

                if (customer != null)
                {
                    type = (customer.Id == 4) ? CustomerOrUser.typeDIT : CustomerOrUser.typeCustomer;
                }
                else
                {
                    Users? user = context.Users.Where(cu => cu.Email.ToLower().Equals(Email.ToLower())).FirstOrDefault();

                    if (user != null)
                    {
                        type = CustomerOrUser.typeCustomer;
                    }
                    else
                    {
                        type = CustomerOrUser.typeUndefined;
                    }
                }

                context.Dispose();
            }

            return type;
        }
    }
}
