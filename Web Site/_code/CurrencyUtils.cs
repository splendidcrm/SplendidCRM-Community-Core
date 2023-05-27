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
using System.Net;
using System.Web;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;

namespace SplendidCRM
{
	/// <summary>
	/// Summary description for Currency.
	/// </summary>
	public class CurrencyUtils
	{
		public static bool bInsideUpdateRates = false;

		private IWebHostEnvironment  hostingEnvironment ;
		private IMemoryCache         memoryCache        ;
		private DbProviderFactories  DbProviderFactories = new DbProviderFactories();
		private HttpApplicationState Application         = new HttpApplicationState();
		private SplendidDefaults     SplendidDefaults    = new SplendidDefaults();
		private HttpContext          Context            ;
		private HttpSessionState     Session            ;
		private Security             Security           ;
		private Sql                  Sql                ;
		private SqlProcs             SqlProcs           ;
		private SplendidError        SplendidError      ;
		private SplendidCache        SplendidCache      ;
		private SplendidDynamic      SplendidDynamic    ;

		public CurrencyUtils(IWebHostEnvironment hostingEnvironment, IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor, HttpSessionState Session, SplendidError SplendidError, SplendidCache SplendidCache, SplendidDynamic SplendidDynamic)
		{
			this.hostingEnvironment  = hostingEnvironment ;
			this.memoryCache         = memoryCache        ;
			this.Context             = httpContextAccessor.HttpContext;
			this.Session             = Session            ;
			this.Security            = new Security(httpContextAccessor, Session);
			this.Sql                 = new Sql(Session, Security);
			this.SqlProcs            = new SqlProcs(Security, Sql);
			this.SplendidError       = SplendidError      ;
			this.SplendidCache       = SplendidCache      ;
			this.SplendidDynamic     = SplendidDynamic    ;
		}

		// 05/02/2016 Paul.  Create a scheduler to ensure that the currencies are always correct. 
		public void UpdateRates(Object sender)
		{
			if ( !bInsideUpdateRates && !Sql.IsEmptyString(Application["CONFIG.CurrencyLayer.AccessKey"]))
			{
				bInsideUpdateRates = true;
				try
				{
					DbProviderFactory dbf = DbProviderFactories.GetFactory();
					using ( IDbConnection con = dbf.CreateConnection() )
					{
						string sSQL;
						sSQL = "select *                  " + ControlChars.CrLf
						     + "  from vwCURRENCIES_List  " + ControlChars.CrLf
						     + " where STATUS  = N'Active'" + ControlChars.CrLf
						     + "   and IS_BASE = 0        " + ControlChars.CrLf
						     + " order by NAME            " + ControlChars.CrLf;
						using ( IDbCommand cmd = con.CreateCommand() )
						{
							cmd.CommandText = sSQL;
							using ( DbDataAdapter da = dbf.CreateDataAdapter() )
							{
								((IDbDataAdapter)da).SelectCommand = cmd;
								using ( DataTable dt = new DataTable() )
								{
									da.Fill(dt);
									foreach ( DataRow row in dt.Rows )
									{
										StringBuilder sbErrors = new StringBuilder();
										string sISO4217 = Sql.ToString(row["ISO4217"]);
										// 12/04/2021 Paul.  Move GetCurrencyConversionRate to Currency object so tha SplendidApp can exclude OrderUtils. 
										float dRate = GetCurrencyConversionRate(sISO4217, sbErrors);
										if ( sbErrors.Length > 0 )
										{
											SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), sbErrors.ToString());
										}
									}
								}
							}
						}
					}
				}
				catch(Exception ex)
				{
					SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), ex);
				}
				finally
				{
					bInsideUpdateRates = false;
				}
			}
		}

		// 12/04/2021 Paul.  Move GetCurrencyConversionRate to Currency object so tha SplendidApp can exclude OrderUtils. 
		public float GetCurrencyConversionRate(string sDestinationCurrency, StringBuilder sbErrors)
		{
			// 04/30/2016 Paul.  Require the Application so that we can get the base currency. 
			string sSourceCurrency = SplendidDefaults.BaseCurrencyISO();
			object oRate = memoryCache.Get("CurrencyLayer." + sSourceCurrency + sDestinationCurrency);
			float dRate = 1.0F;
			if ( oRate == null )
			{
				string sAccessKey      = Sql.ToString (Application["CONFIG.CurrencyLayer.AccessKey"     ]);
				bool   bLogConversions = Sql.ToBoolean(Application["CONFIG.CurrencyLayer.LogConversions"]);
				if ( String.Compare(sSourceCurrency, sDestinationCurrency, true) != 0 )
					dRate = GetCurrencyConversionRate(sAccessKey, bLogConversions, sSourceCurrency, sDestinationCurrency, sbErrors);
			}
			else
			{
				dRate = Sql.ToFloat(oRate);
			}
			return dRate;
		}

		public class CurrencyLayerETag
		{
			public string   ETag;
			public DateTime Date;
			public float    Rate;
		}

		public float GetCurrencyConversionRate(String sAccessKey, bool bLogConversions, string sSourceCurrency, string sDestinationCurrency, StringBuilder sbErrors)
		{
			float dRate = 1.0F;
			try
			{
				if ( String.Compare(sSourceCurrency, sDestinationCurrency, true) == 0 )
				{
					dRate = 1.0F;
				}
				else if ( !Sql.IsEmptyString(sAccessKey) )
				{
					bool bUseEncryptedUrl = Sql.ToBoolean(Application["CONFIG.CurrencyLayer.UseEncryptedUrl"]);
					string sBaseURL = (bUseEncryptedUrl ? "https" : "http") + "://apilayer.net/api/live?access_key=";
					HttpWebRequest objRequest = (HttpWebRequest) WebRequest.Create(sBaseURL + sAccessKey + "&source=" + sSourceCurrency.ToUpper() + "&currencies=" + sDestinationCurrency.ToUpper());
					objRequest.KeepAlive         = false;
					objRequest.AllowAutoRedirect = false;
					objRequest.Timeout           = 15000;  //15 seconds
					objRequest.Method            = "GET";
					// 04/30/2016 Paul.  Support ETags for efficient lookups. 
					CurrencyLayerETag oETag = Application["CurrencyLayer." + sSourceCurrency + sDestinationCurrency] as CurrencyLayerETag;
					if ( oETag != null )
					{
						objRequest.Headers.Add("If-None-Match", oETag.ETag);
						objRequest.IfModifiedSince = oETag.Date;
					}
					using ( HttpWebResponse objResponse = (HttpWebResponse) objRequest.GetResponse() )
					{
						if ( objResponse != null )
						{
							if ( objResponse.StatusCode == HttpStatusCode.OK || objResponse.StatusCode == HttpStatusCode.Found )
							{
								using ( StreamReader readStream = new StreamReader(objResponse.GetResponseStream(), System.Text.Encoding.UTF8) )
								{
									string sJsonResponse = readStream.ReadToEnd();
									JsonDocument json = JsonDocument.Parse(sJsonResponse);
									bool   bSuccess   = json.RootElement.GetProperty("success"  ).GetBoolean();
									string sTimestamp = json.RootElement.GetProperty("timestamp").GetString();
									string sSource    = json.RootElement.GetProperty("source"   ).GetString();
									// {"success":false,"error":{"code":105,"info":"Access Restricted - Your current Subscription Plan does not support HTTPS Encryption."}}
									JsonElement jsonQuotes;
									JsonElement jsonError;
									if ( bSuccess && json.RootElement.TryGetProperty("quotes", out jsonQuotes) )
									{
										dRate = (float) jsonQuotes.GetProperty(sSourceCurrency.ToUpper() + sDestinationCurrency.ToUpper()).GetDecimal();
										int nRateLifetime = Sql.ToInteger(Application["CONFIG.CurrencyLayer.RateLifetime"]);
										if ( nRateLifetime <= 0 )
											nRateLifetime = 90;
										memoryCache.Set("CurrencyLayer." + sSourceCurrency + sDestinationCurrency, dRate, DateTime.Now.AddMinutes(nRateLifetime));
										oETag = new CurrencyLayerETag();
										oETag.ETag = objResponse.Headers.Get("ETag");
										oETag.Rate = dRate;
										DateTime.TryParse(objResponse.Headers.Get("Date"), out oETag.Date);
										Application["CurrencyLayer." + sSourceCurrency + sDestinationCurrency] = oETag;
										
										DbProviderFactory dbf = DbProviderFactories.GetFactory();
										using ( IDbConnection con = dbf.CreateConnection() )
										{
											con.Open();
											using ( IDbTransaction trn = Sql.BeginTransaction(con) )
											{
												try
												{
													Guid gSYSTEM_CURRENCY_LOG = Guid.Empty;
													if ( bLogConversions )
													{
														SqlProcs.spSYSTEM_CURRENCY_LOG_InsertOnly
															( ref gSYSTEM_CURRENCY_LOG
															, "CurrencyLayer"       // SERVICE_NAME
															, sSourceCurrency       // SOURCE_ISO4217
															, sDestinationCurrency  // DESTINATION_ISO4217
															, dRate                 // CONVERSION_RATE
															, sJsonResponse         // RAW_CONTENT
															, trn
															);
													}
													// 04/30/2016 Paul.  We have to update the currency record as it is used inside stored procedures. 
													if ( sSourceCurrency == SplendidDefaults.BaseCurrencyISO() )
													{
														SqlProcs.spCURRENCIES_UpdateRateByISO
															( sDestinationCurrency
															, dRate
															, gSYSTEM_CURRENCY_LOG
															, trn
															);
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
									}
									else if ( json.RootElement.TryGetProperty("error", out jsonError) )
									{
										string sInfo = jsonError.GetProperty("info").GetString();
										sbErrors.Append(sInfo);
									}
									else
									{
										sbErrors.Append("Conversion not found for " + sSourceCurrency + " to " + sDestinationCurrency + ".");
									}
								}
							}
							else if ( objResponse.StatusCode == HttpStatusCode.NotModified )
							{
								dRate = oETag.Rate;
							}
							else
							{
								sbErrors.Append(objResponse.StatusDescription);
							}
						}
					}
				}
				else
				{
					sbErrors.Append("CurrencyLayer access key is empty.");
				}
				if ( sbErrors.Length > 0 )
				{
					SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), "CurrencyLayer " + sSourceCurrency + sDestinationCurrency + ": " + sbErrors.ToString());
				}
			}
			catch(Exception ex)
			{
				sbErrors.AppendLine(ex.Message);
				SplendidError.SystemMessage("Error", new StackTrace(true).GetFrame(0), "CurrencyLayer " + sSourceCurrency + sDestinationCurrency + ": " + Utils.ExpandException(ex));
			}
			return dRate;
		}
	}
}

