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
using System.IO;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SplendidCRM.Pages
{
	// 01/25/2008 Paul.  This page must be accessible without authentication. 
	[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
	public class campaign_trackerv2Model : PageModel
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SqlProcs             SqlProcs           ;
		private SplendidError        SplendidError      ;

		public campaign_trackerv2Model(HttpSessionState Session, Security Security, Sql Sql, SqlProcs SqlProcs, SplendidError SplendidError)
		{
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.Sql                 = Sql                ;
			this.SqlProcs            = SqlProcs           ;
			this.SplendidError       = SplendidError      ;
		}

		public void OnGetAsync()
		{
			SplendidError.SystemMessage("Log", new StackTrace(true).GetFrame(0), "Campaign Tracker v2 " + Request.Query["identifier"] + ", " + Request.Query["track"]);
			Guid gID      = Sql.ToGuid(Request.Query["identifier"]);
			Guid gTrackID = Sql.ToGuid(Request.Query["track"     ]);
			try
			{
				if ( !Sql.IsEmptyGuid(gID) )
				{
					Guid   gTARGET_ID   = Guid.Empty;
					string sTARGET_TYPE = string.Empty;
					SqlProcs.spCAMPAIGN_LOG_UpdateTracker(gID, "link", gTrackID, ref gTARGET_ID, ref sTARGET_TYPE);
				}
				else
				{
					// 09/10/2007 Paul.  Web campaigns will not have an identifier. 
					SqlProcs.spCAMPAIGN_LOG_BannerTracker("link", gTrackID, Sql.ToString(HttpContext.Connection.RemoteIpAddress).ToString());
				}
				if ( !Sql.IsEmptyGuid(gTrackID) )
				{
					DbProviderFactory dbf = DbProviderFactories.GetFactory();
					using ( IDbConnection con = dbf.CreateConnection() )
					{
						con.Open();
						string sSQL ;
						sSQL = "select TRACKER_URL     " + ControlChars.CrLf
						     + "  from vwCAMPAIGN_TRKRS" + ControlChars.CrLf
						     + " where ID = @ID        " + ControlChars.CrLf;
						using ( IDbCommand cmd = con.CreateCommand() )
						{
							cmd.CommandText = sSQL;
							Sql.AddParameter(cmd, "@ID", gTrackID);
							string sTRACKER_URL = Sql.ToString(cmd.ExecuteScalar());
							if ( !Sql.IsEmptyString(sTRACKER_URL) )
								Response.Redirect(sTRACKER_URL);
						}
					}
				}
			}
			catch(Exception ex)
			{
				SplendidError.SystemError(new StackTrace(true).GetFrame(0), ex);
			}
		}
	}
}
