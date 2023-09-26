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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SplendidCRM
{
	// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-5.0&tabs=visual-studio#consuming-a-scoped-service-in-a-background-task
	public class SchedulerHostedService : IHostedService, IDisposable
	{
		private readonly   IServiceProvider                    _serviceProvider;
		private readonly   ILogger<SchedulerHostedService> _logger         ;
		private            Timer                               _timer          ;

		public SchedulerHostedService(IServiceProvider serviceProvider, ILogger<SchedulerHostedService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger          =  logger        ;
		}

		public Task StartAsync(CancellationToken stoppingToken)
		{
			using ( IServiceScope scope = _serviceProvider.CreateScope() )
			{
				SplendidError SplendidError = scope.ServiceProvider.GetRequiredService<SplendidError>();
				SplendidError.SystemWarning(new StackTrace(true).GetFrame(0), "The Scheduler Manager timer has been activated.");
			}
			_timer = new Timer(DoWork, null, new TimeSpan(0, 1, 0), TimeSpan.FromMinutes(5));
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken stoppingToken)
		{
			using ( IServiceScope scope = _serviceProvider.CreateScope() )
			{
				SplendidError SplendidError = scope.ServiceProvider.GetRequiredService<SplendidError>();
				SplendidError.SystemWarning(new StackTrace(true).GetFrame(0), "The Scheduler Manager timer is stopping.");
			}
			_timer?.Change(Timeout.Infinite, 0);
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}

		private void DoWork(object state)
		{
			using ( IServiceScope scope = _serviceProvider.CreateScope() )
			{
				SplendidError SplendidError = scope.ServiceProvider.GetRequiredService<SplendidError>();
				try
				{
					_logger.LogDebug($"SchedulerHostedService.DoWork");
					Debug.WriteLine($"SchedulerHostedService.DoWork");
					SchedulerUtils schedulerUtils = scope.ServiceProvider.GetRequiredService<SchedulerUtils>();
					schedulerUtils.OnTimer();
				}
				catch (Exception ex)
				{
					_logger.LogError($"Failure while processing SchedulerHostedService: {ex.Message}");
					SplendidError.SystemError(new StackTrace(true).GetFrame(0), ex);
				}
			}
		}

	}
}
