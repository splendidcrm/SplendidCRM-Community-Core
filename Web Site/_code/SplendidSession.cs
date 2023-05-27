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
using System.Collections.Generic;
using System.Diagnostics;

namespace SplendidCRM
{
	public class SplendidSession
	{
		private static int nSessionTimeout = 20;
		// 11/16/2014 Paul.  Using a local session variable means that this system will not work on a web farm unless sticky sessions are used. 
		// The alternative is to use the Claims approach of OWIN, but that system seems to be CPU intensive with all the encrypting and decrypting of the claim data. 
		// The claim data is just an encrypted package of non-sensitive user information, such as User ID, User Name and Email. 
		// The claim data is effectively session data that is encrypted and stored in a cookie. 
		private static Dictionary<string, SplendidSession> dictSessions;

		public DateTime Expiration;
		public Guid     USER_ID   ;
		public string   USER_NAME ;

		public static void CreateSession(HttpSessionState Session)
		{
			if ( Session != null )
			{
				if ( dictSessions == null )
				{
					dictSessions = new Dictionary<string, SplendidSession>();
					nSessionTimeout = HttpSessionState.Timeout;
				}
				Guid gUSER_ID = Sql.ToGuid(Session["USER_ID"]);
				if ( !Sql.IsEmptyGuid(gUSER_ID) )
				{
					SplendidSession ss = new SplendidSession();
					ss.Expiration = DateTime.Now.AddMinutes(nSessionTimeout);
					ss.USER_ID   = gUSER_ID;
					ss.USER_NAME = Sql.ToString(Session["USER_NAME"]);
					dictSessions[Session.Id] = ss;
				}
				else
				{
					if ( dictSessions.ContainsKey(Session.Id) )
						dictSessions.Remove(Session.Id);
				}
			}
		}

		public static SplendidSession GetSession(string sSessionID)
		{
			SplendidSession ss = null;
			if ( dictSessions.ContainsKey(sSessionID) )
			{
				ss = dictSessions[sSessionID];
				if ( ss.Expiration < DateTime.Now )
				{
					dictSessions.Remove(sSessionID);
					ss = null;
				}
			}
			return ss;
		}

		public static void PurgeOldSessions(SplendidError SplendidError)
		{
			try
			{
				if ( dictSessions != null )
				{
					DateTime dtNow = DateTime.Now;
					// 11/16/2014 Paul.  We cannot use foreach to remove items from a dictionary, so use a separate list. 
					List<string> arrSessions = new List<string>();
					foreach ( string sSessionID in dictSessions.Keys )
						arrSessions.Add(sSessionID);
					foreach ( string sSessionID in arrSessions )
					{
						SplendidSession ss = dictSessions[sSessionID];
						if ( ss.Expiration < dtNow )
							dictSessions.Remove(sSessionID);
					}
				}
			}
			catch(Exception ex)
			{
				SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), ex);
			}
		}
	}
}

