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

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;

using SplendidCRM;

namespace SplendidWebApi.Controllers
{
	[Authorize]
	[ApiController]
	[Route("Administration/Import/Rest.svc")]
	public class AdministrationImportRestController : ControllerBase
	{
		public const string MODULE_NAME = "Import";
		private IWebHostEnvironment  hostingEnvironment ;
		private IMemoryCache         memoryCache        ;
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpApplicationState Application        = new HttpApplicationState();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private L10N                 L10n               ;
		private Currency             Currency           = new Currency();
		private SplendidCRM.TimeZone TimeZone           = new SplendidCRM.TimeZone();
		private Utils                Utils              ;
		private SqlProcs             SqlProcs           ;
		private SplendidError        SplendidError      ;
		private SplendidCache        SplendidCache      ;
		private RestUtil             RestUtil           ;
		private SplendidImport       SplendidImport     ;
		private SplendidCRM.Crm.Modules          Modules          ;
		private SplendidCRM.Crm.Config           Config           = new SplendidCRM.Crm.Config();

		public AdministrationImportRestController(IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache, HttpSessionState Session, Security Security, Utils Utils, SplendidError SplendidError, SplendidCache SplendidCache, RestUtil RestUtil, SplendidImport SplendidImport, SplendidCRM.Crm.Modules Modules)
		{
			this.hostingEnvironment  = hostingEnvironment ;
			this.memoryCache         = memoryCache        ;
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.L10n                = new L10N(Sql.ToString(Session["USER_LANG"]));
			this.Sql                 = new Sql(Session, Security);
			this.SqlProcs            = new SqlProcs(Security, Sql);
			this.Utils               = Utils              ;
			this.SplendidError       = SplendidError      ;
			this.SplendidCache       = SplendidCache      ;
			this.RestUtil            = RestUtil           ;
			this.SplendidImport      = SplendidImport     ;
			this.Modules             = Modules            ;
		}

		[HttpPost("[action]")]
		[ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
		public void ImportDatabase(bool Truncate, string FILE_MIME_TYPE, string FILE_DATA)
		{
			L10N L10n = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
			if ( !Security.IsAuthenticated() || Security.AdminUserAccess(MODULE_NAME, "edit") < 0 )
			{
				throw(new Exception(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS")));
			}
			if ( Sql.IsEmptyString(FILE_DATA) )
			{
				throw(new Exception("Missing FILE_DATA"));
			}
			
			byte[] byFILE_DATA = new byte[] {};
			if ( !Sql.IsEmptyString(FILE_DATA) )
				byFILE_DATA = Convert.FromBase64String(FILE_DATA);
			using ( MemoryStream stm = new MemoryStream(byFILE_DATA) )
			{
				if ( FILE_MIME_TYPE == "text/xml" )
				{
					using ( MemoryStream mstm = new MemoryStream() )
					{
						using ( BinaryWriter mwtr = new BinaryWriter(mstm) )
						{
							using ( BinaryReader reader = new BinaryReader(stm) )
							{
								byte[] binBYTES = reader.ReadBytes(8*1024);
								while ( binBYTES.Length > 0 )
								{
									for(int i=0; i < binBYTES.Length; i++ )
									{
										// MySQL dump seems to dump binary 0 & 1 for byte values. 
										if ( binBYTES[i] == 0 )
											mstm.WriteByte(Convert.ToByte('0'));
										else if ( binBYTES[i] == 1 )
											mstm.WriteByte(Convert.ToByte('1'));
										else
											mstm.WriteByte(binBYTES[i]);
									}
									binBYTES = reader.ReadBytes(8*1024);
								}
							}
							mwtr.Flush();
							mstm.Seek(0, SeekOrigin.Begin);
							XmlDocument xml = new XmlDocument();
							// 01/20/2015 Paul.  Disable XmlResolver to prevent XML XXE. 
							// https://www.owasp.org/index.php/XML_External_Entity_(XXE)_Processing
							// http://stackoverflow.com/questions/14230988/how-to-prevent-xxe-attack-xmldocument-in-net
							xml.XmlResolver = null;
							xml.Load(mstm);
							SplendidImport.Import(xml, null, Truncate);
						}
					}
				}
				else
				{
					throw(new Exception(L10n.Term("Administration.LBL_IMPORT_DATABASE_ERROR")));
				}
			}
		}
	}
}
