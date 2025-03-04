//
// ConditionOrExpression.cs
//
// Author:
//   Marek Sieradzki (marek.sieradzki@gmail.com)
// 
// (C) 2006 Marek Sieradzki
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.


using System;
using System.Xml;

namespace MonoDevelop.MSBuild.Language.Conditions
{
	internal sealed class ConditionOrExpression : ConditionExpression
	{

		readonly ConditionExpression left;
		readonly ConditionExpression right;

		public ConditionOrExpression (ConditionExpression left, ConditionExpression right)
		{
			this.left = left;
			this.right = right;
		}

		public ConditionExpression Left {
			get { return left; }
		}

		public ConditionExpression Right {
			get { return right; }
		}

		public override bool TryEvaluateToBool (IExpressionContext context, out bool result)
		{
			// Short-circuiting, check only left expr, right
			// would be required only if left == false
			if (!left.TryEvaluateToBool (context, out result))
				return false;

			if (result)
				return true;

			return right.TryEvaluateToBool (context, out result);
		}

		public override void CollectConditionProperties (IPropertyCollector properties)
		{
			left.CollectConditionProperties (properties);
			right.CollectConditionProperties (properties);
		}
	}
}
