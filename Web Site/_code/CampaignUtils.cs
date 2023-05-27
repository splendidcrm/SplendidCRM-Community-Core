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
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace SplendidCRM
{
	/// <summary>
	/// Summary description for Currency.
	/// </summary>
	public class CampaignUtils
	{
		public class SendMail
		{
			private DbProviderFactories  DbProviderFactories = new DbProviderFactories();
			private HttpApplicationState Application         = new HttpApplicationState();
			private HttpSessionState     Session            ;
			private Security             Security           ;
			private Sql                  Sql                ;
			private SqlProcs             SqlProcs           ;
			private SplendidError        SplendidError      ;
			private EmailUtils           EmailUtils         ;

			private Guid                 gID                ;
			private bool                 bTest              ;
			
			public SendMail(IHttpContextAccessor httpContextAccessor, HttpSessionState Session, SplendidError SplendidError, EmailUtils EmailUtils, Guid gID, bool bTest)
			{
				this.Session             = Session            ;
				this.Security            = new Security(httpContextAccessor, Session);
				this.Sql                 = new Sql(Session, Security);
				this.SqlProcs            = new SqlProcs(Security, Sql);
				this.SplendidError       = SplendidError      ;
				this.EmailUtils          = EmailUtils         ;
				this.gID                 = gID                ;
				this.bTest               = bTest              ;
			}
			
#pragma warning disable CS1998
			public async ValueTask QueueStart(CancellationToken token)
			{
				Start();
			}
#pragma warning restore CS1998

			// 06/16/2011 Paul.  Placing the emails in queue can take a long time, so place into a thread. 
			public void Start()
			{
				try
				{
					SplendidError.SystemMessage("Warning", new StackTrace(true).GetFrame(0), "Campaign Start: " + gID.ToString() + " at " + DateTime.Now.ToString() );
					if ( !Sql.IsEmptyGuid(gID) )
					{
						Application["Campaigns." + gID.ToString() + ".Sending"] = true;
						DbProviderFactory dbf = DbProviderFactories.GetFactory();
						using ( IDbConnection con = dbf.CreateConnection() )
						{
							con.Open();
							using ( IDbTransaction trn = Sql.BeginTransaction(con) )
							{
								try
								{
									// 08/22/2011 Paul.  We need to use the command object so that we can increase the timeout. 
									//SqlProcs.spCAMPAIGNS_SendEmail(gID, false, trn);
									using ( IDbCommand cmdCAMPAIGNS_SendEmail = SqlProcs.cmdCAMPAIGNS_SendEmail(con) )
									{
										cmdCAMPAIGNS_SendEmail.Transaction    = trn;
										cmdCAMPAIGNS_SendEmail.CommandTimeout = 0;
										Sql.SetParameter(cmdCAMPAIGNS_SendEmail, "@ID"              , gID             );
										Sql.SetParameter(cmdCAMPAIGNS_SendEmail, "@MODIFIED_USER_ID", Security.USER_ID);
										Sql.SetParameter(cmdCAMPAIGNS_SendEmail, "@TEST"            , bTest           );
										cmdCAMPAIGNS_SendEmail.ExecuteNonQuery();
									}
									trn.Commit();
								}
								catch
								{
									trn.Rollback();
									throw;
								}
							}
						}
						// 12/22/2007 Paul.  Send all queued emails, but include the date so that only these will get sent. 
						// 07/30/2012 Paul.  HttpContext.Current is not valid in a thread.  Must use Context property. 
						if ( bTest )
							EmailUtils.SendQueued(Guid.Empty, gID, false);
					}
					else
					{
						SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), "Invalid Campaign ID.");
					}
				}
				catch(Exception ex)
				{
					SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), Utils.ExpandException(ex));
				}
				finally
				{
					SplendidError.SystemMessage("Warning", new StackTrace(true).GetFrame(0), "Campaign End: " + gID.ToString() + " at " + DateTime.Now.ToString() );
					Application.Remove("Campaigns." + gID.ToString() + ".Sending");
				}
			}
		}

		public class GenerateCalls
		{
			private DbProviderFactories  DbProviderFactories = new DbProviderFactories();
			private HttpApplicationState Application         = new HttpApplicationState();
			private HttpSessionState     Session            ;
			private Security             Security           ;
			private Sql                  Sql                ;
			private SqlProcs             SqlProcs           ;
			private SplendidError        SplendidError      ;
			private EmailUtils           EmailUtils         ;

			private Guid                 gID                ;
			private bool                 bTest              ;
			
			public GenerateCalls(IHttpContextAccessor httpContextAccessor, HttpSessionState Session, SplendidError SplendidError, EmailUtils EmailUtils, Guid gID, bool bTest)
			{
				this.Session             = Session            ;
				this.Security            = new Security(httpContextAccessor, Session);
				this.Sql                 = new Sql(Session, Security);
				this.SqlProcs            = new SqlProcs(Security, Sql);
				this.SplendidError       = SplendidError      ;
				this.EmailUtils          = EmailUtils         ;
				this.gID                 = gID                ;
				this.bTest               = bTest              ;
			}
			
#pragma warning disable CS1998
			public async ValueTask QueueStart(CancellationToken token)
			{
				Start();
			}
#pragma warning restore CS1998

			public void Start()
			{
				try
				{
					SplendidError.SystemMessage("Warning", new StackTrace(true).GetFrame(0), "Campaign Start: " + gID.ToString() + " at " + DateTime.Now.ToString() );
					if ( !Sql.IsEmptyGuid(gID) )
					{
						Application["Campaigns." + gID.ToString() + ".Sending"] = true;
						DbProviderFactory dbf = DbProviderFactories.GetFactory();
						using ( IDbConnection con = dbf.CreateConnection() )
						{
							con.Open();
							using ( IDbTransaction trn = Sql.BeginTransaction(con) )
							{
								try
								{
									// 08/22/2011 Paul.  We need to use the command object so that we can increase the timeout. 
									//SqlProcs.spCAMPAIGNS_GenerateCalls(gID, trn);
									using ( IDbCommand cmdCAMPAIGNS_GenerateCalls = SqlProcs.cmdCAMPAIGNS_GenerateCalls(con) )
									{
										cmdCAMPAIGNS_GenerateCalls.Transaction    = trn;
										cmdCAMPAIGNS_GenerateCalls.CommandTimeout = 0;
										Sql.SetParameter(cmdCAMPAIGNS_GenerateCalls, "@ID"              , gID             );
										Sql.SetParameter(cmdCAMPAIGNS_GenerateCalls, "@MODIFIED_USER_ID", Security.USER_ID);
										cmdCAMPAIGNS_GenerateCalls.ExecuteNonQuery();
									}
									trn.Commit();
								}
								catch
								{
									trn.Rollback();
									throw;
								}
							}
						}
						// 12/22/2007 Paul.  Send all queued emails, but include the date so that only these will get sent. 
						// 07/30/2012 Paul.  HttpContext.Current is not valid in a thread.  Must use Context property. 
						if ( bTest )
							EmailUtils.SendQueued(Guid.Empty, gID, false);
					}
					else
					{
						SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), "Invalid Campaign ID.");
					}
				}
				catch(Exception ex)
				{
					SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), Utils.ExpandException(ex));
				}
				finally
				{
					SplendidError.SystemMessage("Warning", new StackTrace(true).GetFrame(0), "Campaign End: " + gID.ToString() + " at " + DateTime.Now.ToString() );
					Application.Remove("Campaigns." + gID.ToString() + ".Sending");
				}
			}
		}
	}
}

