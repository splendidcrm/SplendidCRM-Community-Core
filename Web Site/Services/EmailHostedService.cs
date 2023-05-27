/**********************************************************************************************************************
 * SplendidCRM is a Customer Relationship Management program created by SplendidCRM Software, Inc. 
 * Copyright (C) 2005-2022 SplendidCRM Software, Inc. All rights reserved.
 * 
 * This program is free software: you can redistribute it and/or modify it under the terms of the 
 * GNU Affero General Public License as published by the Free Software Foundation, either version 3 
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; 
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
 * See the GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License along with this program. 
 * If not, see <http://www.gnu.org/licenses/>. 
 * 
 * You can contact SplendidCRM Software, Inc. at email address support@splendidcrm.com. 
 * 
 * In accordance with Section 7(b) of the GNU Affero General Public License version 3, 
 * the Appropriate Legal Notices must display the following words on all interactive user interfaces: 
 * "Copyright (C) 2005-2011 SplendidCRM Software, Inc. All rights reserved."
 *********************************************************************************************************************/
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SplendidCRM
{
	// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio#consuming-a-scoped-service-in-a-background-task
	public class EmailHostedService : IHostedService, IDisposable
	{
		private readonly   IServiceProvider                _serviceProvider;
		private readonly   ILogger<EmailHostedService> _logger         ;
		private            Timer                           _timer          ;

		public EmailHostedService(IServiceProvider serviceProvider, ILogger<EmailHostedService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger          =  logger        ;
		}

		public Task StartAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("The Email Manager timer has been activated.");
			_timer = new Timer(DoWork, null, new TimeSpan(0, 1, 0), TimeSpan.FromMinutes(1));
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("The Email Manager timer is stopping.");
			_timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		private void DoWork(object state)
		{
			try
			{
				using ( IServiceScope scope = _serviceProvider.CreateScope() )
				{
					_logger.LogDebug($"EmailHostedService.DoWork");
					EmailUtils emailUtils = scope.ServiceProvider.GetRequiredService<EmailUtils>();
					emailUtils.OnTimer();
				}
			}
			catch (Exception ex)
			{
				_logger.LogError($"Failure while processing ArchiveHostedService {ex}");
			}
		}

	}
}
