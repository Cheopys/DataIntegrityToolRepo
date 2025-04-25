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

/*
	This controller is for use of DIT 
 */

namespace DataIntegrityTool.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class CustomersController : ControllerBase
	{
		static Logger logger;
		public CustomersController()
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
		public async Task<Int32> RegisterCustomer(EncryptionWrapperDIT wrapper)
		{
			RegisterCustomerRequest request;

			ServerCryptographyService.DecodeAndDecryptRequest(wrapper, out request);
			return CustomersService.RegisterCustomer(request);
		}

		[HttpGet, Route("GetCustomers")]
		public async Task<List<Customers>> GetCustomers()
		{
			return await CustomersService.GetCustomers();
		}
	}
}
