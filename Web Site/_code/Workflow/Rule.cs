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
using System.Collections.Generic;

namespace SplendidCRM
{
	public class Rule
	{
		public string                   Name                 { get; set; }
		public string                   Description          { get; set; }
		public int                      Priority             { get; set; }
		public RuleReevaluationBehavior ReevaluationBehavior { get; set; }
		public bool                     Active               { get; set; }
		public RuleExpressionCondition  Condition            { get; set; }
		public IList<RuleAction>        ThenActions          { get; set; }
		public IList<RuleAction>        ElseActions          { get; set; }

		public Rule(string name, RuleExpressionCondition condition, List<RuleAction> thenActions, List<RuleAction> elseActions)
		{
			this.Name                 = name          ;
			this.Description          = string.Empty  ;
			this.Priority             = 0             ;
			this.ReevaluationBehavior = RuleReevaluationBehavior.Always;
			this.Active               = true          ;
			this.Condition            = condition     ;
			this.ThenActions          = thenActions   ;
			this.ElseActions          = elseActions   ;
		}

		public void Validate(RuleValidation validation)
		{
			// check the condition
			if ( this.Condition == null )
				validation.Errors.Add(new ValidationError("Messages.MissingRuleCondition"));
			else
				this.Condition.Validate(validation);
 
			// check the optional then actions
			if ( this.ThenActions != null)
				ValidateRuleActions(this.ThenActions, validation);
 
			// check the optional else actions
			if (this.ElseActions != null)
				ValidateRuleActions(this.ElseActions, validation);
		}
 
		private static void ValidateRuleActions(ICollection<RuleAction> ruleActions, RuleValidation validator)
		{
			foreach (RuleAction action in ruleActions)
			{
				action.Validate(validator);
			}
		}
 	}
}
