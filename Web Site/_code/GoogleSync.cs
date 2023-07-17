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
using System.Text;

using Microsoft.AspNetCore.Http;

namespace SplendidCRM
{
	public class GoogleSync
	{
		// 04/23/2010 Paul.  Make the inside flag public so that we can access from the SystemCheck. 
		public  static bool bInsideSyncAll = false;

		public class GoogleWebhook
		{
			public static void ProcessAllNotifications(SplendidError SplendidError, SyncError SyncError)
			{
			}
		}

		public static bool ValidateGoogleApps(HttpApplicationState Application, string sGOOGLE_USERNAME, string sGOOGLE_PASSWORD, StringBuilder sbErrors)
		{
			return false;
		}

		public class UserSync
		{
			public void Start()
			{
			}

			public static UserSync Create(HttpContext Context, Guid gUSER_ID, bool bSyncAll)
			{
				GoogleSync.UserSync User = null;
				return User;
			}
		}
	}
}
