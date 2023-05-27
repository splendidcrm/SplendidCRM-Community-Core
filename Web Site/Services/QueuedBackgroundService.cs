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

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace SplendidCRM
{
	// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio
	public class QueuedBackgroundService : BackgroundService
	{
		private readonly ILogger<QueuedBackgroundService> _logger;
		public IBackgroundTaskQueue TaskQueue { get; }

		public QueuedBackgroundService(IBackgroundTaskQueue taskQueue, ILogger<QueuedBackgroundService> logger)
		{
			TaskQueue = taskQueue;
			_logger = logger;
		}

		public override async Task StartAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("The Queued Hosted Service has been activated.");
			await base.StartAsync(stoppingToken);
		}

		public override async Task StopAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("The Queued Hosted Service is stopping.");

			await base.StopAsync(stoppingToken);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while ( !stoppingToken.IsCancellationRequested )
			{
				var workItem = await TaskQueue.DequeueAsync(stoppingToken);
				try
				{
					_logger.LogInformation("Queued Hosted Service Processing {WorkItem}.", nameof(workItem));
#pragma warning disable CS4014
					// 05/16/2023 Paul.  We don't want to block other work items, so don't await. 
					workItem(stoppingToken);
#pragma warning restore CS4014
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error occurred executing {WorkItem}.", nameof(workItem));
				}
				// 05/23/2023 Paul.  Without delay, loop consumes 100% of resources. 
				await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
			}
		}

	}
}
