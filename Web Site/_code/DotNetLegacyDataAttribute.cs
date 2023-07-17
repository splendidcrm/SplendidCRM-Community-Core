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
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace SplendidCRM
{
	// https://stackoverflow.com/questions/47073838/wrapping-results-of-asp-net-core-webapi-methods-using-iresultfilter

	public class DotNetLegacyData : Dictionary<string, object>
	{
		public DotNetLegacyData(object wrapped)
		{
			this.Add("d", wrapped);
		}
	}

	public class DotNetLegacyDataAttribute : ResultFilterAttribute
	{
		public override void OnResultExecuting(ResultExecutingContext context)
		{
			if ( !(context.ActionDescriptor is ControllerActionDescriptor) )
			{
				return;
			}
			ObjectResult objectResult = context.Result as ObjectResult;
			if ( objectResult == null )
			{
				return;
			}
			if ( !(objectResult.Value is DotNetLegacyData) )
			{
				objectResult.Value        = new DotNetLegacyData(objectResult.Value);
				objectResult.DeclaredType = typeof(DotNetLegacyData);
			}
		}
	}
}
