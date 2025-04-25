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
                Notes        = request.Notes,
                aeskey       = request.aesKey,
                DateAdded    = DateTime.UtcNow,
            };
            using (DataContext context = new())
            {
                context.Customers.Add(customer);

                context.SaveChanges();

                foreach (ToolTypes tooltype in request.Tools)
                {
                    context.AuthorizedToolsCustomer.Add(new AuthorizedToolsCustomer()
                    {
                        CustomerId  = customer.Id,
                        tooltype    = tooltype
                    });
                }

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

                customer.UserLicensingPool = request.UserLicensingPool;
                customer.LicensingIntervalSeconds += request.IntervalSeconds;
                customer.LicensingMeteredCount += request.MeteringCount;

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
	}
}
