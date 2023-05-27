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
using System.Threading.Channels;

namespace SplendidCRM
{
	// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-7.0&tabs=visual-studio
	public interface IBackgroundTaskQueue
	{
		ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);

		ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
	}

	public class BackgroundTaskQueue : IBackgroundTaskQueue
	{
		private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

		public BackgroundTaskQueue()
		{
			// Capacity should be set based on the expected application load and
			// number of concurrent threads accessing the queue.            
			// BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
			// which completes only when space became available. This leads to backpressure,
			// in case too many publishers/calls start accumulating.
			HttpApplicationState Application = new HttpApplicationState();
			int capacity  = Sql.ToInteger(Application["CONFIG.backgroundtask_capacity"]);
			if ( capacity == 0 )
				capacity = 100;
			BoundedChannelOptions options = new BoundedChannelOptions(capacity)
			{
				FullMode = BoundedChannelFullMode.Wait
			};
			_queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
		}

		public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
		{
			if ( workItem == null )
			{
				throw new ArgumentNullException(nameof(workItem));
			}
			await _queue.Writer.WriteAsync(workItem);
		}

		public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
		{
			var workItem = await _queue.Reader.ReadAsync(cancellationToken);
			return workItem;
		}
	}
}
