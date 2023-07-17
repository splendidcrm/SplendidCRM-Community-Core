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
using System.Linq;
using System.Diagnostics;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;

namespace SplendidCRM
{
	// 07/09/2023 Paul.  Instead of using Security.IsAuthenticated(), use attribute. 
	public class SplendidSessionAuthorizeAttribute: Attribute, IAuthorizationFilter
	{
		private HttpApplicationState Application = new HttpApplicationState();

		public SplendidSessionAuthorizeAttribute()
		{
		}

		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
			if ( allowAnonymous )
				return;
			
			if ( context != null )
			{
				bool bIsAuthenticated = false;
				if ( context.HttpContext != null )
				{
					if ( context.HttpContext.Session != null )
					{
						string sUSER_ID = context.HttpContext.Session.GetString("USER_ID");
						if ( !Sql.IsEmptyString(sUSER_ID) )
						{
							bIsAuthenticated = true;
						}
					}
				}
				if ( !bIsAuthenticated )
				{
					L10N L10n = new L10N(Sql.ToString(Application["CONFIG.default_language"]));
					context.Result = new UnauthorizedObjectResult("SplendidSessionAuthorize: " + L10n.Term("ACL.LBL_INSUFFICIENT_ACCESS"));
				}
			}
		}
	}
}
