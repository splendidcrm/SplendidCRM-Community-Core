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
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace SplendidCRM.Pages.Documents
{
	[Authorize]
	[SplendidSessionAuthorize]
	public class DocumentModel : PageModel
	{
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SplendidError        SplendidError      ;

		public DocumentModel(HttpSessionState Session, Security Security, Sql Sql, SplendidError SplendidError)
		{
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.Sql                 = Sql                ;
			this.SplendidError       = SplendidError      ;
		}

		// 10/20/2009 Paul.  Move blob logic to WriteStream. 
		public static void WriteStream(Guid gID, IDbConnection con, BinaryWriter writer)
		{
			// 09/06/2008 Paul.  PostgreSQL does not require that we stream the bytes, so lets explore doing this for all platforms. 
			if ( Sql.StreamBlobs(con) )
			{
				using ( IDbCommand cmd = con.CreateCommand() )
				{
					cmd.CommandText = "spDOCUMENTS_CONTENT_ReadOffset";
					cmd.CommandType = CommandType.StoredProcedure;
					
					const int BUFFER_LENGTH = 4*1024;
					int idx  = 0;
					int size = 0;
					byte[] binData = new byte[BUFFER_LENGTH];  // 10/20/2005 Paul.  This allocation is only used to set the parameter size. 
					IDbDataParameter parID          = Sql.AddParameter(cmd, "@ID"         , gID    );
					IDbDataParameter parFILE_OFFSET = Sql.AddParameter(cmd, "@FILE_OFFSET", idx    );
					// 01/21/2006 Paul.  Field was renamed to READ_SIZE. 
					IDbDataParameter parREAD_SIZE   = Sql.AddParameter(cmd, "@READ_SIZE"  , size   );
					IDbDataParameter parBYTES       = Sql.AddParameter(cmd, "@BYTES"      , binData);
					parBYTES.Direction = ParameterDirection.InputOutput;
					do
					{
						parID         .Value = gID          ;
						parFILE_OFFSET.Value = idx          ;
						parREAD_SIZE  .Value = BUFFER_LENGTH;
						size = 0;
						// 08/14/2005 Paul.  Oracle returns the bytes in a field.
						// SQL Server can only return the bytes in a resultset. 
						// 10/20/2005 Paul.  MySQL works returning bytes in an output parameter. 
						// 02/05/2006 Paul.  DB2 returns bytse in a field. 
						if ( Sql.IsOracle(cmd) || Sql.IsDB2(cmd) ) // || Sql.IsMySQL(cmd) )
						{
							cmd.ExecuteNonQuery();
							binData = Sql.ToByteArray(parBYTES);
							if ( binData != null )
							{
								size = binData.Length;
								writer.Write(binData);
								idx += size;
							}
						}
						else
						{
							using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
							{
								if ( rdr.Read() )
								{
									// 10/20/2005 Paul.  MySQL works returning a record set, but it cannot be cast to a byte array. 
									// binData = (byte[]) rdr[0];
									binData = Sql.ToByteArray((System.Array) rdr[0]);
									if ( binData != null )
									{
										size = binData.Length;
										writer.Write(binData);
										idx += size;
									}
								}
							}
						}
					}
					while ( size == BUFFER_LENGTH );
				}
			}
			else
			{
				using ( IDbCommand cmd = con.CreateCommand() )
				{
					string sSQL;
					sSQL = "select CONTENT                     " + ControlChars.CrLf
					     + "  from vwDOCUMENT_REVISIONS_CONTENT" + ControlChars.CrLf
					     + " where ID = @ID                    " + ControlChars.CrLf;
					Sql.AddParameter(cmd, "@ID", gID);
					cmd.CommandText = sSQL;
					//object oBlob = cmd.ExecuteScalar();
					//byte[] binData = Sql.ToByteArray(oBlob);
					//writer.Write(binData);
					using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
					{
						if ( rdr.Read() )
						{
							// 10/20/2009 Paul.  Try to be more efficient by using a reader. 
							Sql.WriteStream(rdr, 0, writer);
						}
					}
				}
			}
		}

		public IActionResult OnGetAsync()
		{
			string sMessage = "Image not found.";
			try
			{
				// 05/05/2010 Paul.  Lets make it easy to get the current revision of a document. 
				Guid gID = Sql.ToGuid(Request.Query["ID"]);
				Guid gDOCUMENT_ID = Sql.ToGuid(Request.Query["DOCUMENT_ID"]);
				//if ( !IsPostBack )
				{
					if ( !Sql.IsEmptyGuid(gID) || !Sql.IsEmptyGuid(gDOCUMENT_ID) )
					{
						SplendidCRM.DbProviderFactory dbf = DbProviderFactories.GetFactory();
						using ( IDbConnection con = dbf.CreateConnection() )
						{
							con.Open();
							string sSQL ;
							Guid gDOCUMENT_REVISION_ID = Guid.Empty;
							if ( !Sql.IsEmptyGuid(gDOCUMENT_ID) )
							{
								sSQL = "select DOCUMENT_REVISION_ID" + ControlChars.CrLf
								     + "     , FILE_MIME_TYPE      " + ControlChars.CrLf
								     + "     , FILENAME            " + ControlChars.CrLf
								     + "  from vwDOCUMENTS         " + ControlChars.CrLf;
								using ( IDbCommand cmd = con.CreateCommand() )
								{
									cmd.CommandText = sSQL;
									Security.Filter(cmd, "Documents", "view");
									Sql.AppendParameter(cmd, gDOCUMENT_ID, "ID", false);
									using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
									{
										if ( rdr.Read() )
										{
											gDOCUMENT_REVISION_ID = Sql.ToGuid(rdr["DOCUMENT_REVISION_ID"]);
											Response.ContentType = Sql.ToString(rdr["FILE_MIME_TYPE"]);
											// 01/27/2011 Paul.  Don't use GetFileName as the name may contain reserved directory characters, but expect them to be removed in Utils.ContentDispositionEncode. 
											string sFileName = Sql.ToString(rdr["FILENAME"]);
											// 08/06/2008 yxy21969.  Make sure to encode all URLs.
											// 12/20/2009 Paul.  Use our own encoding so that a space does not get converted to a +. 
											Response.Headers.Add("Content-Disposition", "attachment;filename=" + Utils.ContentDispositionEncode(sFileName));
										}
									}
								}
							}
							else
							{
								sSQL = "select ID                  " + ControlChars.CrLf
								     + "     , FILE_MIME_TYPE      " + ControlChars.CrLf
								     + "     , FILENAME            " + ControlChars.CrLf
								     + "  from vwDOCUMENT_REVISIONS" + ControlChars.CrLf
								     + " where ID = @ID            " + ControlChars.CrLf;
								using ( IDbCommand cmd = con.CreateCommand() )
								{
									cmd.CommandText = sSQL;
									Sql.AddParameter(cmd, "@ID", gID);
									using ( IDataReader rdr = cmd.ExecuteReader(CommandBehavior.SingleRow) )
									{
										if ( rdr.Read() )
										{
											gDOCUMENT_REVISION_ID = Sql.ToGuid(rdr["ID"]);
											Response.ContentType = Sql.ToString(rdr["FILE_MIME_TYPE"]);
											// 01/27/2011 Paul.  Don't use GetFileName as the name may contain reserved directory characters, but expect them to be removed in Utils.ContentDispositionEncode. 
											string sFileName = Sql.ToString(rdr["FILENAME"]);
											// 08/06/2008 yxy21969.  Make sure to encode all URLs.
											// 12/20/2009 Paul.  Use our own encoding so that a space does not get converted to a +. 
											Response.Headers.Add("Content-Disposition", "attachment;filename=" + Utils.ContentDispositionEncode(sFileName));
										}
									}
								}
							}
							if ( !Sql.IsEmptyGuid(gDOCUMENT_REVISION_ID) )
							{
								using ( MemoryStream mem = new MemoryStream() )
								{
									using ( BinaryWriter writer = new BinaryWriter(mem) )
									{
										WriteStream(gDOCUMENT_REVISION_ID, con, writer);
										mem.Position = 0;
										byte[] b = mem.ToArray();
										return File(b, Response.ContentType);
									}
								}
							}
							else
							{
								// 04/30/2010 Paul.  Image not found is correct, unless we are an Offline Client. 
								if ( !Sql.IsEmptyString(Sql.ToString(Session["SystemSync.Server"])) )
									sMessage = "Must be online to retrieve image.";
								
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
			Response.ContentType = "text/plain";
			return File(data, Response.ContentType);
		}
	}
}
