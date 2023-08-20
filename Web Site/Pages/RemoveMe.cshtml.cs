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
	public class RemoveMeModel : PageModel
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SqlProcs             SqlProcs           ;
		private SplendidError        SplendidError      ;
		private L10N                 L10n               ;

		public  string               litREMOVE_ME_HEADER { get; set; }
		public  string               lblError            { get; set; }
		public  string               lblWarning          { get; set; }
		public  string               litREMOVE_ME_FOOTER { get; set; }

		public RemoveMeModel(HttpSessionState Session, Security Security, Sql Sql, SqlProcs SqlProcs, SplendidError SplendidError)
		{
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.Sql                 = Sql                ;
			this.SqlProcs            = SqlProcs           ;
			this.SplendidError       = SplendidError      ;
			this.L10n                = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
		}

		public void OnGetAsync()
		{
			SplendidError.SystemMessage("Log", new StackTrace(true).GetFrame(0), "Remove Me " + Request.Query["identifier"]);
			try
			{
				Guid gID = Sql.ToGuid(Request.Query["identifier"]);
				if ( !Sql.IsEmptyGuid(gID) )
				{
					Guid   gTARGET_ID   = Guid.Empty;
					string sTARGET_TYPE = string.Empty;
					SqlProcs.spCAMPAIGN_LOG_UpdateTracker(gID, "removed", Guid.Empty, ref gTARGET_ID, ref sTARGET_TYPE);
					if ( sTARGET_TYPE == "Users" )
					{
						lblError = L10n.Term("Campaigns.LBL_USERS_CANNOT_OPTOUT");
					}
					else
					{
						litREMOVE_ME_HEADER = L10n.Term("Campaigns.LBL_REMOVE_ME_HEADER_STEP1");
						litREMOVE_ME_FOOTER = L10n.Term("Campaigns.LBL_REMOVE_ME_FOOTER_STEP1");
						SqlProcs.spCAMPAIGNS_OptOut(gTARGET_ID, sTARGET_TYPE);
						//lblError.Text = L10n.Term("Campaigns.LBL_ELECTED_TO_OPTOUT");
					}
				}
				// 11/23/2012 Paul.  Skip during precompile. 
				else if ( !Sql.ToBoolean(Request.Query["PrecompileOnly"]) )
				{
					// 11/23/2012 Paul.  Don't use the standard error label as it will cause the precompile to stop. 
					lblWarning = L10n.Term("Campaigns.LBL_REMOVE_ME_INVALID_IDENTIFIER");
				}
			}
			catch(Exception ex)
			{
				SplendidError.SystemError(new StackTrace(true).GetFrame(0), ex);
			}
		}
	}
}
