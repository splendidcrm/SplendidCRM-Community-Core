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
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;

namespace SplendidCRM.Controllers.Campaigns
{
	[Authorize]
	[SplendidSessionAuthorize]
	[ApiController]
	[Route("Campaigns/Rest.svc")]
	public class RestController : ControllerBase
	{
		private IHttpContextAccessor httpContextAccessor;
		private SplendidCRM.DbProviderFactories  DbProviderFactories = new SplendidCRM.DbProviderFactories();
		private HttpApplicationState Application        = new HttpApplicationState();
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SqlProcs             SqlProcs           ;
		private L10N                 L10n               ;
		private SplendidError        SplendidError      ;
		private EmailUtils           EmailUtils         ;
		private SplendidCRM.Crm.Modules          Modules          ;
		private IBackgroundTaskQueue taskQueue          ;

		public RestController(IHttpContextAccessor httpContextAccessor, HttpSessionState Session, Security Security, Sql Sql, SqlProcs SqlProcs, SplendidError SplendidError, EmailUtils EmailUtils, SplendidCRM.Crm.Modules Modules, IBackgroundTaskQueue taskQueue)
		{
			this.httpContextAccessor = httpContextAccessor;
			this.Session             = Session            ;
			this.Security            = Security           ;
			this.L10n                = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
			this.Sql                 = Sql                ;
			this.SqlProcs            = SqlProcs           ;
			this.SplendidError       = SplendidError      ;
			this.EmailUtils          = EmailUtils         ;
			this.Modules             = Modules            ;
			this.taskQueue           = taskQueue          ;
		}

		private DataRow GetCampaign(Guid gID)
		{
			DataRow rdr = null;
			string m_sMODULE    = "Campaigns";
			string m_sVIEW_NAME = "vw" + Modules.TableName(m_sMODULE) + "_Edit";
			SplendidCRM.DbProviderFactory dbf = DbProviderFactories.GetFactory();
			using ( IDbConnection con = dbf.CreateConnection() )
			{
				string sSQL ;
				sSQL = "select *"               + ControlChars.CrLf
				     + "  from " + m_sVIEW_NAME + ControlChars.CrLf;
				using ( IDbCommand cmd = con.CreateCommand() )
				{
					cmd.CommandText = sSQL;
					Security.Filter(cmd, m_sMODULE, "view");
					Sql.AppendParameter(cmd, gID, "ID", false);
					con.Open();

					using ( DbDataAdapter da = dbf.CreateDataAdapter() )
					{
						((IDbDataAdapter)da).SelectCommand = cmd;
						using ( DataTable dtCurrent = new DataTable() )
						{
							da.Fill(dtCurrent);
							if ( dtCurrent.Rows.Count > 0 && (Security.GetRecordAccess(dtCurrent.Rows[0], m_sMODULE, "view", "ASSIGNED_USER_ID") >= 0) )
							{
								rdr = dtCurrent.Rows[0];
							}
						}
					}
				}
			}
			return rdr;
		}

		[HttpPost("[action]")]
		public async Task<Dictionary<string, object>> SendTest()
		{
			if ( !Security.IsAuthenticated() || Security.GetUserAccess("Campaigns", "view") < 0 )
			{
				throw(new Exception(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS")));
			}
			
			string sStatus = String.Empty;
			Guid gID = Sql.ToGuid(Request.Query["ID"]);
			if ( !Sql.IsEmptyGuid(gID) )
			{
				DataRow rdr = GetCampaign(gID);
				if ( rdr != null )
				{
					string sCAMPAIGN_TYPE = Sql.ToString(rdr["CAMPAIGN_TYPE"]);
					if ( sCAMPAIGN_TYPE == "Email" )
					{
						// 05/18/2012 Paul.  Even a test can timeout, so use thread. 
						if ( !Sql.ToBoolean(Application["Campaigns." + gID.ToString() + ".Sending"]) )
						{
							// 06/16/2011 Paul.  Placing the emails in queue can take a long time, so place into a thread. 
							// 08/22/2011 Paul.  We need to use a class so that we can pass the context and the ID. 
							CampaignUtils.SendMail send = new CampaignUtils.SendMail(Session, Security, Sql, SqlProcs, SplendidError, EmailUtils, gID, true);
							await taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
							// 08/22/2011 Paul.  The SendEmail thread will be aborted if we redirect the page. 
							sStatus = L10n.Term("Campaigns.LBL_SENDING");
						}
						else
						{
							sStatus = L10n.Term("Campaigns.ERR_SENDING_NOW");
						}
					}
					else
					{
						throw(new Exception("This operation is not supported for campaign type " + sCAMPAIGN_TYPE));
					}
				}
				else
				{
					throw(new Exception(L10n.Term("ACL.LBL_NO_ACCESS")));
				}
			}
			else
			{
				throw(new Exception("ID is empty"));
			}
			Dictionary<string, object> d = new Dictionary<string, object>();
			d.Add("d", sStatus);
			return d;
		}

		[HttpPost("[action]")]
		public async Task<Dictionary<string, object>> SendEmail()
		{
			if ( !Security.IsAuthenticated() || Security.GetUserAccess("Campaigns", "view") < 0 )
			{
				throw(new Exception(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS")));
			}
			
			string sStatus = String.Empty;
			Guid gID = Sql.ToGuid(Request.Query["ID"]);
			if ( !Sql.IsEmptyGuid(gID) )
			{
				DataRow rdr = GetCampaign(gID);
				if ( rdr != null )
				{
					string sCAMPAIGN_TYPE = Sql.ToString(rdr["CAMPAIGN_TYPE"]);
					if ( sCAMPAIGN_TYPE == "Email" )
					{
						// 05/18/2012 Paul.  Even a test can timeout, so use thread. 
						if ( !Sql.ToBoolean(Application["Campaigns." + gID.ToString() + ".Sending"]) )
						{
							// 06/16/2011 Paul.  Placing the emails in queue can take a long time, so place into a thread. 
							// 08/22/2011 Paul.  We need to use a class so that we can pass the context and the ID. 
							CampaignUtils.SendMail send = new CampaignUtils.SendMail(Session, Security, Sql, SqlProcs, SplendidError, EmailUtils, gID, false);
							await taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
							// 08/22/2011 Paul.  The SendEmail thread will be aborted if we redirect the page. 
							sStatus = L10n.Term("Campaigns.LBL_SENDING");
						}
						else
						{
							sStatus = L10n.Term("Campaigns.ERR_SENDING_NOW");
						}
					}
					else
					{
						throw(new Exception("This operation is not supported for campaign type " + sCAMPAIGN_TYPE));
					}
				}
				else
				{
					throw(new Exception(L10n.Term("ACL.LBL_NO_ACCESS")));
				}
			}
			else
			{
				throw(new Exception("ID is empty"));
			}
			Dictionary<string, object> d = new Dictionary<string, object>();
			d.Add("d", sStatus);
			return d;
		}

		[HttpPost("[action]")]
		public async Task<Dictionary<string, object>> GenerateCalls()
		{
			if ( !Security.IsAuthenticated() || Security.GetUserAccess("Campaigns", "view") < 0 )
			{
				throw(new Exception(L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS")));
			}
			
			string sStatus = String.Empty;
			Guid gID = Sql.ToGuid(Request.Query["ID"]);
			if ( !Sql.IsEmptyGuid(gID) )
			{
				DataRow rdr = GetCampaign(gID);
				if ( rdr != null )
				{
					string sCAMPAIGN_TYPE = Sql.ToString(rdr["CAMPAIGN_TYPE"]);
					if ( sCAMPAIGN_TYPE == "Telesales" )
					{
						if ( !Sql.ToBoolean(Application["Campaigns." + gID.ToString() + ".Sending"]) )
						{
							CampaignUtils.GenerateCalls send = new CampaignUtils.GenerateCalls(Session, Security, Sql, SqlProcs, SplendidError, EmailUtils, gID, false);
							await taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
							sStatus = L10n.Term("Campaigns.LBL_SENDING");
						}
						else
						{
							sStatus = L10n.Term("Campaigns.ERR_SENDING_NOW");
						}
					}
					else
					{
						throw(new Exception("This operation is not supported for campaign type " + sCAMPAIGN_TYPE));
					}
				}
				else
				{
					throw(new Exception(L10n.Term("ACL.LBL_NO_ACCESS")));
				}
			}
			else
			{
				throw(new Exception("ID is empty"));
			}
			Dictionary<string, object> d = new Dictionary<string, object>();
			d.Add("d", sStatus);
			return d;
		}

	}
}
