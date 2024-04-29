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
using DocumentFormat.OpenXml.Wordprocessing;

namespace SplendidCRM
{
	public interface IChatServer
	{
		string JoinGroup(string sConnectionId, string sGroupName);
	}

	public interface IChatClient
	{
		void newMessage(Guid gCHAT_CHANNEL_ID, Guid gID, string sNAME, string sDESCRIPTION, string sDATE_ENTERED, Guid gPARENT_ID, string sPARENT_TYPE, string sPARENT_NAME, Guid gCREATED_BY_ID, string sCREATED_BY, string sCREATED_BY_PICTURE, Guid gNOTE_ATTACHMENT_ID, string sFILENAME, string sFILE_EXT, string sFILE_MIME_TYPE, long lFILE_SIZE, bool bATTACHMENT_READY);
	}

	/// <summary>
	/// Summary description for ChatManagerHub.
	/// </summary>
	public class ChatManagerHub : Hub<IChatServer>
	{
		private ChatManager _chatManager;

		public ChatManagerHub(ChatManager chatManager)
		{
			this._chatManager = chatManager;
		}

		public async Task<string> JoinGroup(string sGroupName)
		{
			if ( !Sql.IsEmptyString(sGroupName) )
			{
				string[] arrTracks = sGroupName.Split(',');
				foreach ( string sTrack in arrTracks )
				{
					await Groups.AddToGroupAsync(Context.ConnectionId, sTrack);
				}
				return Context.ConnectionId + " joined " + sGroupName;
			}
			return "Group not specified.";
		}
	}
}

