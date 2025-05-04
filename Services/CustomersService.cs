using System.Drawing;
using System.Globalization;
using System.Net;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.Runtime.Internal;
using DataIntegrityTool.Db;
using DataIntegrityTool.Schema;
using DataIntegrityTool.Services;
using Humanizer;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.LayoutRenderers;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using NuGet.Packaging;
using NuGet.Versioning;

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

        public static Int32 RegisterCustomer(RegisterCustomerRequest request)
        {
            Customers customer = new Customers()
            {
                Name         = request.Name,
                Description  = request.Description,
                EmailContact = request.EmailContact,
                PasswordHash = request.PasswordHash,
                Notes        = request.Notes,
                AesKey       = request.AesKey,
                DateAdded    = DateTime.UtcNow,
                UsageSince   = DateTime.MinValue,
                UserLicensingPool        = false,
                LicensingIntervalSeconds = 0,
                LicensingMeteredCount    = 0,
            };

            Users user = new Users()
            {
                AesKey                   = request.AesKey,
                Email                    = request.EmailContact,
                Name                     = request.Name,
                PasswordHash             = request.PasswordHash,
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

                context.Users.Add(user);

                context.SaveChanges();

                context.SaveChanges();
                context.Dispose();
            }

            return customer.Id;
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
                CustomerId = request.CustomerId
            };

            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Find(request.CustomerId);

                customer.UserLicensingPool         = request.UserLicensingPool;
                customer.LicensingIntervalSeconds += request.IntervalSeconds;
                customer.LicensingMeteredCount    += request.MeteringCount;

                response.MeteringCount = customer.LicensingMeteredCount;
                response.IntervalSeconds = customer.LicensingIntervalSeconds;

                if (customer.UserLicensingPool)
                {
                    foreach (UserLicenseAllocation ula in request.userLicenseAllocations)
                    {
                        Users? user = context.Users.Where(us => us.Id.Equals(ula.UserId)).FirstOrDefault();

                        if (user != null)
                        {
                            user.LicensingMeteredCount += ula.UserMeteringCount;
                            user.LicensingIntervalSeconds += ula.UserIntervalSeconds;
                        }
                    }
                }

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

            usage.IntervalSessions = intervals.Count();
            
            foreach (LicenseInterval interval in intervals)
            {
                if (interval.TimeBegin != null
                && interval.TimeEnd != null)
                {
                    DateTime timeBegin = interval.TimeBegin.Value;
                    DateTime timeEnd   = interval.TimeEnd;
                    usage.IntervalSeconds += (Int32)(timeEnd.Subtract(timeBegin)).TotalSeconds;
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


        public static ErrorCodes AddNewUserTokens(List<UserRegistration> registrations)
        {
            ErrorCodes errorcode = ErrorCodes.errorNone;

            using (DataContext context = new())
            {
                context.UserRegistration.AddRange(registrations);
            }

            return errorcode;
        }
    }
}
