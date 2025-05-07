using System.Text.Json;
using System.Net;
using DataIntegrityTool.Schema;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataIntegrityTool.Services;
using DataIntegrityTool.Shared;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using Amazon.Runtime.Internal;
using NLog;
using System.Runtime.Intrinsics.Arm;

/*
	This controller is for use of DIT 
 */

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class TestController : ControllerBase
	{
		static Logger logger;
		public TestController()
		{
			var config = new NLog.Config.LoggingConfiguration();

			// Targets where to log to: File and Console
			var logconsole = new NLog.Targets.ConsoleTarget("logconsole");

			// Rules for mapping loggers to targets            
			config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, logconsole);

            // Apply config           
            LogManager.Configuration = config;
			logger = LogManager.GetCurrentClassLogger();
		}

		[HttpPut, Route("RegisterCustomer")]
		public async Task<Int32> RegisterCustomer(RegisterCustomerRequest request)
		{
			return CustomersService.RegisterCustomer(request);
		}

		[HttpGet, Route("GetCustomers")]
		[Produces("application/json")]
		public async Task<string> GetCustomers()
		{
			List<Customers> customers = await CustomersService.GetCustomers();

			return JsonSeriaizer.Serialize(customers);
		}
        [HttpGet, Route("GetUsers")]
        [Produces("application/json")]
        public async Task<string> GetUsers(Int32 CustomerId)
        {
            List<Users> users = await UsersService.GetUsers(CustomerId);

			return JsonSerializer.Serialize(users);
        }
    }
}
