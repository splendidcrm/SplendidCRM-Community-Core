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
using System.Xml;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SplendidCRM.Pages
{
	public class SystemCheckModel : PageModel
	{
		private IWebHostEnvironment  hostingEnvironment ;
		private IMemoryCache         memoryCache        ;
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpContext          Context            ;
		private HttpApplicationState Application        = new HttpApplicationState();
		private HttpSessionState     Session            ;
		private Security             Security           ;

		public  HttpApplicationState application        { get { return Application    ; } }
		public  HttpSessionState     session            { get { return Session        ; } }
		public  Security             security           { get { return Security       ; } }
		public  HttpRequest          request            { get { return Request        ; } }
		
		public  CultureInfo          culture            { get; set; }
		public  string               MachineName        { get; set; }
		public  string               SqlVersion         { get; set; }
		public  string               LastError          { get; set; }
		public  string               AUTH_USER          { get; set; }

		public SystemCheckModel(IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache, HttpSessionState Session, Security Security)
		{
			this.hostingEnvironment  = hostingEnvironment ;
			this.memoryCache         = memoryCache        ;
			this.Context             = httpContextAccessor.HttpContext;
			this.Session             = Session            ;
			this.Security            = Security           ;
		}

		public void OnGet()
		{
			string m_sCULTURE     = SplendidCRM.Sql.ToString (Session["USER_SETTINGS/CULTURE"]);
			SplendidCRM.L10N L10n = new SplendidCRM.L10N(m_sCULTURE);
			culture = CultureInfo.CreateSpecificCulture(L10n.NAME);

			AUTH_USER = String.Empty;  //Sql.ToString(Context.Request.ServerVariables["AUTH_USER"]);
			if ( Context.User != null && Context.User.Identity != null )
			{
				AUTH_USER = Context.User.Identity.Name;
			}

			MachineName = System.Environment.MachineName;
			try
			{
				// 11/20/2005 Paul.  ASP.NET 2.0 has a namespace conflict, so we need the full name for the SplendidCRM factory. 
				SplendidCRM.DbProviderFactory dbf = DbProviderFactories.GetFactory();
				using ( IDbConnection con = dbf.CreateConnection() )
				{
					con.Open();
					// 09/27/2009 Paul.  Show SQL version. 
					if ( Sql.IsSQLServer(con) )
					{
						using ( IDbCommand cmd = con.CreateCommand() )
						{
							cmd.CommandText = "select @@VERSION";
							SqlVersion = Sql.ToString(cmd.ExecuteScalar());
						}
					}
				}
			}
			catch(Exception ex)
			{
				LastError = ex.Message;
			}
		}
	}
}
