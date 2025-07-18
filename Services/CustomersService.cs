﻿using Amazon.Runtime.Internal;
using Amazon.S3.Model;
using Amazon.SimpleNotificationService.Model;
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

			using (DataContext context = new())
			{
				SubscriptionTypes type = context.SubscriptionTypes.Where(st => st.Id.Equals(request.SubscriptionId)).FirstOrDefault();
                Users user = null;
                Customers customer = new Customers()
                {
                    NameFirst    = request.NameFirst,
                    NameLast     = request.NameLast,
                    Company      = request.Company,
                    Email        = request.Email,
                    PasswordHash = ServerCryptographyService.SHA256(request.Password),
                    Notes        = request.Notes,
                    AesKey       = Convert.FromHexString(request.AesKey),
                    DateAdded    = DateTime.UtcNow,
                    UsageSince   = DateTime.MinValue,
                    Tools        = request.Tools,
                    SeatsMax     = 10,
                    Scans        = type.scans,
                    SubscriptionTime = TimeSpan.FromDays(type.days)
                };
    
				context.Customers.Add(customer);

                // need the new customer PK to continue

				context.SaveChanges();

				response.CustomerId = customer.Id;

                context.Add(new CustomerSubscriptions()
                {
                    CustomerId     = customer.Id,
					SubscriptionId = request.SubscriptionId,
					ExpirationDate = null
				});

				if (request.InitialUser)
                {
                    user = new Users()
                    {
                        AesKey       = Convert.FromHexString(request.AesKey),
                        Email        = request.Email,
                        NameFirst    = request.NameFirst,
                        NameLast     = request.NameLast,
                        PasswordHash = ServerCryptographyService.SHA256(request.Password),
                        DateAdded    = DateTime.UtcNow,
                        Tools        = request.Tools,
                    };
                }

                if (user != null)
                {
                    user.CustomerId = customer.Id;
                    context.Users.Add(user);
                }
                context.SaveChanges();
                context.Dispose();
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
                    response.Error = ErrorCodes.errorInvalidUser;
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

        public static void UpdateCustomer(UpdateCustomerRequest request)
        {
            using (DataContext context = new())
            {
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(request.Id)).FirstOrDefault();

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
                Customers? customer = context.Customers.Where(cu => cu.Id.Equals(CustomerId)).FirstOrDefault();
                List<Users> users = context.Users.Where(us => us.CustomerId.Equals(CustomerId)).ToList();
                List<Session> sessions = context.Session.Where(s => s.CustomerId.Equals(CustomerId)).ToList();
                List<LicenseMetered> licensesetered = context.LicenseMetered.Where(lm => lm.CustomerId.Equals(CustomerId)).ToList();

                context.Remove(customer);
                context.RemoveRange(users);
                context.RemoveRange(sessions);
                context.RemoveRange(licensesetered);

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
            usage.MeteringCount = metereds.Count();

            earliest = metereds.Min(lm => lm.TimeBegun.Value);

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
                    type = (customer.Id == 4) ? LoginType.typeDIT : LoginType.typeCustomer;
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
                                                    Int16 count)
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
                    customer.Scans += count;

                    context.SaveChanges();

                    response.ScansAfter = customer.Scans;
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
                Customers?         customer     = context.Customers            .Where(cu => cu.Id        .Equals(CustomerId))    .FirstOrDefault();
                SubscriptionTypes? subscription = context.SubscriptionTypes    .Where(st => st.Id        .Equals(subscriptionId)).FirstOrDefault();
                CustomerSubscriptions custsub   = context.CustomerSubscriptions.Where(cs => cs.CustomerId.Equals(CustomerId))    .FirstOrDefault();

                if (customer != null)
                {
                    // ExpirationDate is null for a subscription that hasn't been used yet

					if (custsub.ExpirationDate    == null
                    &&  customer.SubscriptionTime != null)
					{
                        custsub.ExpirationDate = DateTime.UtcNow + (TimeSpan.FromDays(subscription.days) + customer.SubscriptionTime);
						customer.SubscriptionTime = null;
					}
                    else
                    {
						custsub.ExpirationDate += TimeSpan.FromDays(subscription.days);
						customer.SubscriptionTime = null;
					}

					customer.Scans += subscription.scans;

                    context.SaveChanges();

                    response.Expiration = custsub.ExpirationDate.Value;
                    response.ScansAfter = customer.Scans;
                }
                else
                {
                    response.Error = ErrorCodes.errorInvalidCustomerId;
                }

                context.Dispose();
            }

            return response;
        }
    }
}

