﻿using SimpleRules.Attributes;
using SimpleRules.Contracts;
using SimpleRules.Exceptions;
using SimpleRules.Generic;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace SimpleRules.Handlers
{
    public class SimpleRuleHandler : IHandler
    {
        public EvaluatedRule GenerateEvaluatedRule<TConcrete>(BaseRuleAttribute attribute, PropertyInfo targetProp)
        {
            var expressions = new List<Expression>();
            var messages = new List<string>();
            var relationalAttr = attribute as RelationalOperatorAttribute;
            var srcType = typeof (TConcrete);
            var input = Expression.Parameter(srcType, "i");
            var leftExpr = Expression.PropertyOrField(input, targetProp.Name);
            var isNullable = targetProp.IsNullable();
            if (relationalAttr.CanBeNull)
            {
                if (isNullable)
                {
                    var nullConstant = leftExpr.CreateBinaryExpression(targetProp);
                    expressions.Add(nullConstant);
                    messages.Add("It can also be null.");
                }
                else
                {
                    throw new NotNullablePropertyException(targetProp.Name);
                }
            }
            if (relationalAttr.ConstantValue != null)
            {
                var constantValue = Expression.Constant(relationalAttr.ConstantValue);
                BinaryExpression finalExpr = leftExpr.CreateBinaryExpression(constantValue, relationalAttr.SupportedType, targetProp);                
                expressions.Add(finalExpr);
                messages.Add(string.Format("Or {0} can be {1} {2}.", targetProp.Name.AddSpaces(), relationalAttr.SupportedType.ToString().AddSpaces(), relationalAttr.ConstantValue));
            }
            if (!string.IsNullOrEmpty(relationalAttr.OtherPropertyName))
            {
                var otherPropInfo = srcType.GetProperty(relationalAttr.OtherPropertyName);
                var rightExpr = Expression.PropertyOrField(input, relationalAttr.OtherPropertyName);
                var propExpr = leftExpr.CreateBinaryExpression(rightExpr, relationalAttr.SupportedType, targetProp, otherPropInfo);
                expressions.Add(propExpr);
                messages.Add(string.Format("{0} should be {1} the {2}.", targetProp.Name.AddSpaces(), relationalAttr.SupportedType.ToString().AddSpaces(), relationalAttr.OtherPropertyName.AddSpaces()));
            }
            var orExpr = expressions.CreateBinaryExpression();
            var lambdaExpr = Expression.Lambda(orExpr, input);
            messages.Reverse();
            return new EvaluatedRule
            {
                MessageFormat = string.Join(" ", messages),
                Expression = lambdaExpr,
                RuleType = relationalAttr.RuleType
            };
        }

        public bool Handles(BaseRuleAttribute attribute)
        {
            return typeof(RelationalOperatorAttribute).IsAssignableFrom(attribute.GetType());
        }
    }
}
