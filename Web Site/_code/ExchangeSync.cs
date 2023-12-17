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
using Microsoft.AspNetCore.Http;

namespace SplendidCRM
{
	public class ExchangeSync
	{
		public class UserSync
		{
			public void Start()
			{
			}

			public static UserSync Create(HttpContext Context, Guid gUSER_ID, bool bSyncAll)
			{
				ExchangeSync.UserSync User = null;
				return User;
			}

			public UserSync(HttpSessionState Session, Security Security, Sql Sql, SqlProcs SqlProcs, SplendidError SplendidError, XmlUtil XmlUtil, SyncError SyncError, ExchangeSecurity ExchangeSecurity, ExchangeUtils ExchangeUtils, ExchangeSync ExchangeSync, string sEXCHANGE_ALIAS, string sEXCHANGE_EMAIL, string sMAIL_SMTPUSER, string sMAIL_SMTPPASS, Guid gUSER_ID, string sEXCHANGE_WATERMARK, bool bSyncAll, bool bOFFICE365_OAUTH_ENABLED)
			{
			}
		}
		
		public static void UnsyncContact(Guid gUSER_ID, Guid gCONTACT_ID)
		{
		}
	}
}
