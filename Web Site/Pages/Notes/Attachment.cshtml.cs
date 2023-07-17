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
using System.Diagnostics;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SplendidCRM.Pages.Notes
{
	[Authorize]
	[SplendidSessionAuthorize]
	public class AttachmentModel : PageModel
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SplendidError        SplendidError      ;

		public AttachmentModel(HttpSessionState Session, Security Security, Sql Sql, SplendidError SplendidError)
		{
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.Sql                 = Sql                ;
			this.SplendidError       = SplendidError      ;
		}

		public IActionResult OnGetAsync()
		{
			string sMessage = "Image not found.";
			try
			{
				Guid gID = Sql.ToGuid(Request.Query["ID"]);
				//if ( !IsPostBack )
				{
					if ( !Sql.IsEmptyGuid(gID) )
					{
						DbProviderFactory dbf = DbProviderFactories.GetFactory();
						using ( IDbConnection con = dbf.CreateConnection() )
						{
							con.Open();
							string sSQL ;
							sSQL = "select *                 " + ControlChars.CrLf
							     + "  from vwNOTE_ATTACHMENTS" + ControlChars.CrLf
							     + " where ID = @ID          " + ControlChars.CrLf;
							using ( IDbCommand cmd = con.CreateCommand() )
							{
								cmd.CommandText = sSQL;
								Sql.AddParameter(cmd, "@ID", gID);
								using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
								{
									if ( rdr.Read() )
									{
										Response.ContentType = Sql.ToString(rdr["FILE_MIME_TYPE"]);
										// 01/27/2011 Paul.  Don't use GetFileName as the name may contain reserved directory characters, but expect them to be removed in Utils.ContentDispositionEncode. 
										string sFileName = Sql.ToString(rdr["FILENAME"]);
										// 08/06/2008 yxy21969.  Make sure to encode all URLs.
										// 12/20/2009 Paul.  Use our own encoding so that a space does not get converted to a +. 
										Response.Headers.Add("Content-Disposition", "attachment;filename=" + Utils.ContentDispositionEncode(sFileName));
									}
								}
							}
							try
							{
								using ( MemoryStream mem = new MemoryStream() )
								{
									using ( BinaryWriter writer = new BinaryWriter(mem) )
									{
										// 10/20/2009 Paul.  Move blob logic to WriteStream. 
										// 10/30/2021 Paul.  Move WriteStream to ModuleUtils. 
										ModuleUtils.Notes.Attachment.WriteStream(gID, con, writer);
										mem.Position = 0;
										byte[] b = mem.ToArray();
										return File(b, Response.ContentType);
									}
								}
							}
							catch(Exception ex)
							{
								// 08/29/2010 Paul.  Convert the error message to an image. 
								sMessage = "Error: " + ex.Message;
							}
						}
					}
				}
			}
			catch(Exception ex)
			{
				SplendidError.SystemError(new StackTrace(true).GetFrame(0), ex);
				sMessage = ex.Message;
			}
			byte[] data = System.Text.Encoding.UTF8.GetBytes(sMessage);
			Response.ContentType = "text/plain";  // "image/gif"
			return File(data, Response.ContentType);
		}
	}
}
