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
using System.Threading;
using System.Threading.Tasks;
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
	[Route("Campaigns/Rest.svc")]
	public class CampaignsRestController : ControllerBase
	{
		private HttpContext          Context            ;
		private IHttpContextAccessor httpContextAccessor;
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
		private EmailUtils           EmailUtils         ;
		private SplendidCRM.Crm.Modules          Modules          ;
		private SplendidCRM.Crm.Config           Config           = new SplendidCRM.Crm.Config();
		private IBackgroundTaskQueue             _taskQueue       ;

		public CampaignsRestController(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache, HttpSessionState Session, Security Security, Utils Utils, SplendidError SplendidError, SplendidCache SplendidCache, RestUtil RestUtil, EmailUtils EmailUtils, SplendidCRM.Crm.Modules Modules, IBackgroundTaskQueue taskQueue)
		{
			this.Context             = this.HttpContext   ;
			this.httpContextAccessor = httpContextAccessor;
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
			this.EmailUtils          = EmailUtils         ;
			this.Modules             = Modules            ;
			this._taskQueue          = taskQueue          ;
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
		public async Task<string> SendTest()
		{
			L10N L10n = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
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
							CampaignUtils.SendMail send = new CampaignUtils.SendMail(httpContextAccessor, Session, SplendidError, EmailUtils, gID, true);
							await _taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
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
			return sStatus;
		}

		[HttpPost("[action]")]
		public async Task<string> SendEmail()
		{
			L10N L10n = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
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
							CampaignUtils.SendMail send = new CampaignUtils.SendMail(httpContextAccessor, Session, SplendidError, EmailUtils, gID, false);
							await _taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
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
			return sStatus;
		}

		[HttpPost("[action]")]
		public async Task<string> GenerateCalls()
		{
			L10N L10n = new L10N(Sql.ToString(Session["USER_SETTINGS/CULTURE"]));
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
							CampaignUtils.GenerateCalls send = new CampaignUtils.GenerateCalls(httpContextAccessor, Session, SplendidError, EmailUtils, gID, false);
							await _taskQueue.QueueBackgroundWorkItemAsync(send.QueueStart);
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
			return sStatus;
		}

	}
}
