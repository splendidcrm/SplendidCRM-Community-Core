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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace SplendidCRM
{
	public class RuleSet
	{
		public string               Name        { get; set; }
		public string               Description { get; set; }
		public RuleChainingBehavior Behavior    { get; set; }
		public List<Rule>           Rules       { get; set; }

		public RuleSet(string name)
		{
			this.Behavior = RuleChainingBehavior.Full;
			this.Rules = new List<Rule>();
		}

		public bool Validate(RuleValidation validation)
		{
			if ( validation == null )
				throw new ArgumentNullException("validation");
 
			foreach ( Rule r in this.Rules )
			{
				r.Validate(validation);
			}
 
			if ( validation.Errors == null || validation.Errors.Count == 0 )
				return true;
 
			return false;
		}

		public void Execute(RuleExecution exec)
		{
			try
			{
				foreach ( Rule r in this.Rules )
				{
					bool bCondition = r.Condition.Evaluate(exec);
					if ( bCondition )
					{
						foreach ( RuleAction action in r.ThenActions )
						{
							action.Execute(exec);
						}
					}
					else
					{
						foreach ( RuleAction action in r.ElseActions )
						{
							action.Execute(exec);
						}
					}
				}
			}
			catch(Exception ex)
			{
				if ( exec.ThisObject is SqlObj )
				{
					(exec.ThisObject as SqlObj).ErrorMessage = ex.Message;
				}
				ValidationError error = new ValidationError(ex.Message);
				exec.Validation.Errors.Add(error);
			}
		}
	}
}
