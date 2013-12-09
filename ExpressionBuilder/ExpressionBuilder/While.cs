﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ExpressionBuilder.Fluent;
using ExpressionBuilder.Parser;
using ExpressionBuilder.CodeLines;

namespace ExpressionBuilder
{
	public class While : IWhile, ICodeLine
	{
		internal List<ICodeLine> CodeLines;
		internal Condition Condition;

		internal While(Condition condition)
		{
			if (condition == null) throw new ArgumentException();
			Condition = condition;
			CodeLines = new List<ICodeLine>();
		}

		public ICodeLine Do(ICodeLine firstCodeLine, params ICodeLine[] codeLines)
		{
			CodeLines.Add(firstCodeLine);
			foreach (var codeLine in codeLines)
			{
				CodeLines.Add(codeLine);
			}
			return this;
		}


		public string ToString(ParseContext context)
		{
			var result = "while(" + Condition.ToString(context) + ")\n";
			result += context.Pad + "{\n";
			context.AddLevel();

			foreach (var line in CodeLines)
			{
				var createVariable = line as CreateVariable;
				if (createVariable != null)
				{
					createVariable.DefaultInitialize(context);
				}
				result += context.Pad + line.ToString(context) + ";\n";
			}

			context.RemoveLevel();
			result += context.Pad + "}";
			return result;
		}

		public void PreParseExpression(ParseContext context)
		{
			//var pl = context.Current;
			Condition.PreParseExpression(context);
			context.AddLevel();

			foreach (var line in CodeLines)
			{
				line.PreParseExpression(context);
			}

			context.RemoveLevel();
		}

		public Type ParsedType { get; private set; }

		public Expression ToExpression(ParseContext context)
		{
			var conditionExpression = Condition.ToExpression(context);
			context.AddLevel();

			var thenLine = new List<Expression>();
			var listOfThenVars = new List<ParameterExpression>();
			foreach (var line in CodeLines)
			{
				var expLine = line.ToExpression(context);

				var createVariable = line as CreateVariable;
				if (createVariable != null)
				{
					listOfThenVars.Add((ParameterExpression)expLine);
					expLine = createVariable.DefaultInitialize(context);
				}
				thenLine.Add(expLine);
			}
			var thenBlock = Expression.Block(listOfThenVars.ToArray(), thenLine);

			context.RemoveLevel();

			LabelTarget label = Expression.Label(Guid.NewGuid().ToString());
			var ifThenElse = Expression.IfThenElse(
																conditionExpression,
																thenBlock,
																Expression.Break(label));
			return Expression.Loop(ifThenElse, label);
		}
	}
}
