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
using System.Text;
using System.Data;
using System.Text.Json;

using Microsoft.AspNetCore.Http;

namespace SplendidCRM
{
	public class HttpSessionState
	{
		public static int Timeout = 20;
		private HttpContext          Context    ;

		public HttpSessionState(IHttpContextAccessor httpContextAccessor)
		{
			this.Context     = httpContextAccessor.HttpContext;
		}

		// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-6.0
		public object this[string key]
		{
			get
			{
				object obj = null;
				try
				{
					// 06/18/2023 Paul.  Debugging with IIS express fails with Context == null. 
					if ( this.Context != null )
					{
						string value = this.Context.Session.GetString(key);
						if ( value != null )
						{
							// 12/26/2021 Paul.  JsonSerializer.Deserialize is returning JsonElement, which does not convert well to boolean. 
							if ( value == "true" )
								obj = true;
							else if ( value == "false" )
								obj = false;
							else
								obj = JsonSerializer.Deserialize<object>(value);
						}
					}
				}
				catch
				{
				}
				return obj;
			}
			set
			{
				if ( value != null )
				{
					if ( value.GetType() == typeof(DataTable) )
						throw(new Exception("HttpSessionState: Use Get/Set to serialize DataTable"));
					this.Context.Session.SetString(key, JsonSerializer.Serialize(value));
				}
				else
				{
					this.Context.Session.SetString(key, null);
				}
			}
		}

		// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/app-state?view=aspnetcore-7.0
		public void SetTable(string key, DataTable value)
		{
			if ( value != null )
			{
				// 06/09/2023 Paul.  Must have table name to serialize. 
				if ( Sql.IsEmptyString(value.TableName) )
				{
					value.TableName = "[" + key + "]";
				}
				StringBuilder sb = new StringBuilder();
				using ( StringWriter wtr = new StringWriter(sb, System.Globalization.CultureInfo.InvariantCulture) )
				{
					(value as DataTable).WriteXml(wtr, XmlWriteMode.WriteSchema, false);
				}
				this.Context.Session.SetString(key, sb.ToString());
			}
			else
			{
				this.Context.Session.SetString(key, null);
			}
		}

		public DataTable GetTable(string key)
		{
			DataTable dt = null;
			string value = this.Context.Session.GetString(key);
			if ( value != null )
			{
				dt = new DataTable();
				using ( StringReader srdr = new StringReader(value) )
				{
					dt.ReadXml(srdr);
				}
			}
			return dt;
		}

		public void Remove(string key)
		{
			this.Context.Session.Remove(key);
		}

		public void Clear()
		{
			this.Context.Session.Clear();
		}

		public string Id
		{
			get { return this.Context.Session.Id; }
		}
	}
}
