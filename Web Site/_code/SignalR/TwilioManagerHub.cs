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
using System.Data;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

using Twilio.Base;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;

namespace SplendidCRM
{
	public interface ITwilioServer
	{
		string JoinGroup(string sConnectionId, string sGroupName);
		Guid CreateSmsMessage(string sMESSAGE_SID, string sFROM_NUMBER, string sTO_NUMBER, string sSUBJECT);
	}

	public interface ITwilioClient
	{
		void incomingMessage(string sMESSAGE_SID, string sFROM_NUMBER, string sTO_NUMBER, string sSUBJECT, object sSMS_MESSAGE_ID);
	}

	/// <summary>
	/// Summary description for TwilioManagerHub.
	/// </summary>
	public class TwilioManagerHub : Hub<ITwilioServer>
	{
		private TwilioManager _twilioManager;

		public TwilioManagerHub(TwilioManager twilioManager)
		{
			this._twilioManager = twilioManager;
		}

		public async Task<string> JoinGroup(string sGroupName)
		{
			if ( !Sql.IsEmptyString(sGroupName) )
			{
				sGroupName = Utils.NormalizePhone(TwilioManager.RemoveCountryCode(sGroupName));
				await Groups.AddToGroupAsync(Context.ConnectionId, sGroupName);
				return Context.ConnectionId + " joined " + sGroupName;
			}
			return "Group not specified.";
		}

		public async Task<Guid> CreateSmsMessage(string sMESSAGE_SID, string sFROM_NUMBER, string sTO_NUMBER, string sSUBJECT)
		{
			return await _twilioManager.CreateSmsMessage(sMESSAGE_SID, sFROM_NUMBER, sTO_NUMBER, sSUBJECT, String.Empty, String.Empty);
		}
	}
}

