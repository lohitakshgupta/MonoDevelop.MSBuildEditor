// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.MSBuild.Language.Expressions;

using NUnit.Framework;

using static MonoDevelop.MSBuild.Language.ExpressionCompletion;

namespace MonoDevelop.MSBuild.Tests
{
	[TestFixture]
	class ExpressionCompletion
	{
		// params are: document text, typedChar, trigger result, length
		//    typedChar and length can be omitted and default to \0 and zero
		//	  if typedChar is \0, it's treated as explicitly invoking the command
		//    if typedChar is provided, it's added to the document text

		static object[] BareValueTestCases = {
			//explicitly trigger in bare value
			new object[] { "", TriggerState.Value },
			new object[] { "abc", TriggerState.Value, 3 },
			new object[] { "abcde", TriggerState.Value, 5 },
			new object[] { " ", TriggerState.Value },
			new object[] { "  xyz", TriggerState.Value, 3 },

			//start typing new bare value
			new object[] { "", 'a', TriggerState.Value, 1 },

			//start typing invalid char
			new object[] { "", '/', TriggerState.None },

			//typing within bare value
			new object[] { "a", 'x', TriggerState.None },
			new object[] { "$", 'x', TriggerState.None },
		};

		static object[] PropertyTestCases = {
			//start typing property
			new object[] { "", '$', TriggerState.PropertyOrValue, 1 },

			//explicit trigger after property start
			new object[] { "$", TriggerState.PropertyOrValue, 1 },

			//auto trigger property name on typing
			new object[] { "$", '(', TriggerState.PropertyName },
			new object[] { "$(", 'a', TriggerState.PropertyName, 1 },

			//explicit trigger in property name
			new object[] { "$(", TriggerState.PropertyName },
			new object[] { "$(abc", TriggerState.PropertyName, 3 },
			new object[] { "$(abcefgh", TriggerState.PropertyName, 7 },

			//explicit trigger after invalid char in property name
			new object[] { "$(a-", TriggerState.None },

			//type char in property name
			new object[] { "$(a", 'b', TriggerState.None },
			new object[] { "$(abc", '$', TriggerState.None },
		};

		static object[] ItemTestCases = {
			//start typing item
			new object[] { "", '@', TriggerState.ItemOrValue, 1 },

			//explicit trigger after item start
			new object[] { "@", TriggerState.ItemOrValue, 1 },

			//auto trigger item name on typing
			new object[] { "@", '(', TriggerState.ItemName },
			new object[] { "@(", 'a', TriggerState.ItemName, 1 },

			//explicit trigger in item name
			new object[] { "@(", TriggerState.ItemName },
			new object[] { "@(abc", TriggerState.ItemName, 3 },
			new object[] { "@(abcefgh", TriggerState.ItemName, 7 },

			//explicit trigger after invalid char in item name
			new object[] { "@(a-", TriggerState.None },

			//type char in item name
			new object[] { "@(a", 'b', TriggerState.None },
			new object[] { "@(abc", '$', TriggerState.None },
		};

		static object[] MetadataTestCases = {
			// note metadata allows a surprising amount of whitespace, unlike properties and items
			//start typing metadata
			new object[] { "", '%', TriggerState.MetadataOrValue, 1 },

			//explicit trigger after metadata start
			new object[] { "%", TriggerState.MetadataOrValue, 1 },

			//auto trigger metadata name on typing
			new object[] { "%", '(', TriggerState.MetadataOrItemName },
			new object[] { "%(", 'a', TriggerState.MetadataOrItemName, 1 },
			new object[] { "%(  ", 'a', TriggerState.MetadataOrItemName, 1 },

			//explicit trigger in metadata name
			new object[] { "%(", TriggerState.MetadataOrItemName },
			new object[] { "%(   ", TriggerState.MetadataOrItemName },
			new object[] { "%(abc", TriggerState.MetadataOrItemName, 3 },
			new object[] { "%(  abc", TriggerState.MetadataOrItemName, 3 },
			new object[] { "%(abcefgh", TriggerState.MetadataOrItemName, 7 },

			//explicit trigger after invalid char in metadata name
			new object[] { "%(a-", TriggerState.None },

			//type char in metadata name
			new object[] { "%(a", 'b', TriggerState.None },
			new object[] { "%(abc", '$', TriggerState.None },
		};

		static object[] QualifiedMetadataTestCases = {
			// note metadata allows a surprising amount of whitespace, unlike properties and items

			// explicit trigger qualified metadata name
			new object[] { "%(foo.", TriggerState.MetadataName },
			new object[] { "%(  foo.", TriggerState.MetadataName },
			new object[] { "%(foo .", TriggerState.MetadataName },
			new object[] { "%(foo.ab", TriggerState.MetadataName, 2 },
			new object[] { "%(foo.abcde", TriggerState.MetadataName, 5 },
			new object[] { "%(foo  .abcd", TriggerState.MetadataName, 4 },
			new object[] { "%(foo  .  abc", TriggerState.MetadataName, 3 },

			// automatic trigger qualified metadata name
			new object[] { "%(foo", '.', TriggerState.MetadataName },
			new object[] { "%(foo ", '.', TriggerState.MetadataName },
			new object[] { "%(foo.", 'a', TriggerState.MetadataName, 1 },
			new object[] { "%(foo  .", 'a', TriggerState.MetadataName, 1 },
			new object[] { "%(foo  .  ", 'a', TriggerState.MetadataName, 1 },

			//explicit trigger after invalid char in qualified metadata name
			new object[] { "%(a.b-", TriggerState.None },
			new object[] { "%(a  .b-", TriggerState.None },

			//type char in qualified metadata name
			new object[] { "%(ab.cd", 'e', TriggerState.None },
			new object[] { "%(ab.cd", '$', TriggerState.None },
			new object[] { "%(ab .cd", 'e', TriggerState.None },
			new object[] { "%(ab  .cd", '$', TriggerState.None },
		};

		static object[] PropertyFunctionTestCases = {
			// explicit trigger property function name
			new object[] { "$(foo.", TriggerState.PropertyFunctionName },
			new object[] { "$(foo .", TriggerState.PropertyFunctionName },
			new object[] { "$(foo.ab", TriggerState.PropertyFunctionName, 2 },
			new object[] { "$(foo.abcde", TriggerState.PropertyFunctionName, 5 },
			new object[] { "$(foo  .abcd", TriggerState.PropertyFunctionName, 4 },

			// automatic trigger property function name
			new object[] { "$(foo", '.', TriggerState.PropertyFunctionName },
			new object[] { "$(foo ", '.', TriggerState.PropertyFunctionName },
			new object[] { "$(foo.", 'a', TriggerState.PropertyFunctionName, 1 },
			new object[] { "$(foo  .", 'a', TriggerState.PropertyFunctionName, 1 },

			//explicit trigger after invalid char in property function name
			new object[] { "$(a.b-", TriggerState.None },
			new object[] { "$(a  .b-", TriggerState.None },

			//type char in property function name
			new object[] { "$(ab.cd", 'e', TriggerState.None },
			new object[] { "$(ab.cd", '$', TriggerState.None },
			new object[] { "$(ab .cd", 'e', TriggerState.None },
			new object[] { "$(ab  .cd", '$', TriggerState.None },

			//explicit trigger after indexer
			new object[] { "$(a[0].", TriggerState.PropertyFunctionName },
			new object[] { "$(a[0].bcd", TriggerState.PropertyFunctionName, 3 },

			//automatic trigger after indexer
			new object[] { "$(a[0]", '.', TriggerState.PropertyFunctionName },
			new object[] { "$(a[0].", 'a', TriggerState.PropertyFunctionName, 1 },
		};

		static object[] ItemFunctionTestCases = {
			// explicit trigger item function name
			new object[] { "@(foo->", TriggerState.ItemFunctionName },
			new object[] { "@(foo ->", TriggerState.ItemFunctionName },
			new object[] { "@(foo->ab", TriggerState.ItemFunctionName, 2 },
			new object[] { "@(foo->abcde", TriggerState.ItemFunctionName, 5 },
			new object[] { "@(foo  ->abcd", TriggerState.ItemFunctionName, 4 },

			// automatic trigger item function name
			new object[] { "@(foo-", '>', TriggerState.ItemFunctionName },
			new object[] { "@(foo -", '>', TriggerState.ItemFunctionName },
			new object[] { "@(foo->", 'a', TriggerState.ItemFunctionName, 1 },
			new object[] { "@(foo  ->", 'a', TriggerState.ItemFunctionName, 1 },

			//explicit trigger after invalid char in item function name
			new object[] { "@(a->b/", TriggerState.None },

			//type char in item function name
			new object[] { "@(ab->cd", 'e', TriggerState.None },
			new object[] { "@(ab->cd", '$', TriggerState.None },
			new object[] { "@(ab ->cd", 'e', TriggerState.None },
			new object[] { "@(ab  ->cd", '$', TriggerState.None },
		};

		static object[] StaticPropertyFunctionTestCases = {
			// explicit trigger static property function name
			new object[] { "$([Foo]::", TriggerState.PropertyFunctionName },
			new object[] { "$([Foo]  ::", TriggerState.None }, //space between ] and :: is invalid
			new object[] { "$([Foo]::ab", TriggerState.PropertyFunctionName, 2 },
			new object[] { "$([Foo]::abcde", TriggerState.PropertyFunctionName, 5 },

			// automatic trigger static property function name
			new object[] { "$([Foo]:", ':', TriggerState.PropertyFunctionName },
			new object[] { "$([Foo] :", ':', TriggerState.None }, //space between ] and :: is invalid
			new object[] { "$([Foo]::", 'a', TriggerState.PropertyFunctionName, 1 },
			new object[] { "$([Foo]  ::", 'a', TriggerState.None },

			//explicit trigger after invalid char in static property function name
			new object[] { "$([Foo]::b-", TriggerState.None },
			new object[] { "$([Foo]  ::b-", TriggerState.None },

			//type char in static property function name
			new object[] { "$([Foo]::cd", 'e', TriggerState.None },
			new object[] { "$([Foo]::cd", '$', TriggerState.None },
			new object[] { "$([Foo] ::cd", 'e', TriggerState.None },
			new object[] { "$([Foo]   :: cd", '$', TriggerState.None },
		};

		static object[] StaticPropertyFunctionNameTestCases = {
			//auto trigger static property function class name on typing
			new object[] { "$(", '[', TriggerState.PropertyFunctionClassName },
			new object[] { "$([", 'a', TriggerState.PropertyFunctionClassName, 1 },

			//explicit trigger in static property function class name
			new object[] { "$([", TriggerState.PropertyFunctionClassName },
			new object[] { "$([abc", TriggerState.PropertyFunctionClassName, 3 },
			new object[] { "$([abcefgh", TriggerState.PropertyFunctionClassName, 7 },

			//explicit trigger after invalid char in static property function class name
			new object[] { "$([a-", TriggerState.None },

			//type char in static property function class name
			new object[] { "$([a", 'b', TriggerState.None },
			new object[] { "$([abc", '$', TriggerState.None }
		};

		static object[] SemicolonListValueTestCases = new object[][] {
			new[] {
				// automatic trigger after list separator
				new object[] { "foo", ';', TriggerState.Value},
				// explicit trigger after list separator
				new object[] { "foo;", TriggerState.Value},
			},
			PrependExpression ("foo;", BareValueTestCases),
			PrependExpression ("foo;", PropertyTestCases)
		}.SelectMany (x => x).ToArray ();

		static object[] CommaListValueTestCases = new object[][] {
			new[] {
				// automatic trigger after list separator
				new object[] { "foo", ',', TriggerState.Value},
				// explicit trigger after list separator
				new object[] { "foo,", TriggerState.Value},
			},
			PrependExpression ("foo,", BareValueTestCases),
			PrependExpression ("foo,", PropertyTestCases)
		}.SelectMany (x => x).ToArray ();

		static object[] ChangeTriggers (object [] arr, TriggerState from, TriggerState to)
		{
			foreach (object[] subArr in arr) {
				for (var i = 0; i < subArr.Length; i++) {
					if (subArr[i] is TriggerState t && t == from) {
						subArr[i] = to;
					}
				}
			}
			return arr;
		}

		static object[] PrependExpression (string v, object[] arr)
			=> arr.Select (a => {
				var testCase = (object[])a;
				var newTestCase = new object[testCase.Length];
				newTestCase[0] = v + (string)testCase[0];
				Array.Copy (testCase, 1, newTestCase, 1, testCase.Length - 1);
				return newTestCase;
			}).ToArray ();

		static object[] PropertyFunctionArgumentTestCases = new object[][] {
			PrependExpression ("$(foo.bar('", BareValueTestCases),
			PrependExpression ("$(foo.bar('", PropertyTestCases),
			PrependExpression ("$(foo.bar('", MetadataTestCases),
			PrependExpression ("$(foo.bar('", QualifiedMetadataTestCases),
			ChangeTriggers (PrependExpression ("$(foo.bar(", PropertyTestCases), TriggerState.Value, TriggerState.BareFunctionArgumentValue),
			ChangeTriggers (PrependExpression ("$(foo.bar(", BareValueTestCases), TriggerState.Value, TriggerState.BareFunctionArgumentValue),
			PrependExpression ("$(foo.bar(1, '", BareValueTestCases),
			PrependExpression ("$(foo.bar(1, '", PropertyTestCases),
		}.SelectMany (x => x).ToArray ();

		static object[] ItemFunctionArgumentTestCases = new object[][] {
			PrependExpression ("@(a->'", PropertyTestCases),
			PrependExpression ("@(a->'", MetadataTestCases),
		}.SelectMany (x => x).ToArray ();

		static object[] ExpressionTestCases = new object[][] {
			BareValueTestCases,
			PropertyTestCases,
			ItemTestCases,
			MetadataTestCases,
			QualifiedMetadataTestCases,
			PropertyFunctionTestCases,
			ItemFunctionTestCases,
			StaticPropertyFunctionTestCases,
			StaticPropertyFunctionNameTestCases,
			PropertyFunctionArgumentTestCases,
			ItemFunctionArgumentTestCases,
			SemicolonListValueTestCases,
			CommaListValueTestCases,
		}.SelectMany (x => x).ToArray ();

		[Test]
		[TestCaseSource ("ExpressionTestCases")]
		public void TestTriggering (object[] args)
		{
			string expr = (string)args[0];
			char typedChar = (args[1] as char?) ?? '\0';
			var expectedState = (TriggerState)(args[1] is char ? args[2] : args[1]);
			int expectedLength = args[args.Length - 1] as int? ?? 0;

			if (typedChar != '\0') {
				expr += typedChar;
			}

			var state = GetTriggerState (
				expr, typedChar, false,
				out int triggerLength, out ExpressionNode triggerNode,
				out ListKind listKind,
				out IReadOnlyList<ExpressionNode> comparandVariables
			);

			Assert.AreEqual (expectedState, state);
			Assert.AreEqual (expectedLength, triggerLength);
		}

		[TestCase ("", TriggerState.Value, 0)]
		[TestCase ("$(", TriggerState.PropertyName, 0)]
		[TestCase ("$(Foo) == '", TriggerState.Value, 0, "Foo")]
		[TestCase ("$(Foo) == '$(", TriggerState.PropertyName, 0, "Foo")]
		[TestCase ("$(Foo) == '$(a", TriggerState.PropertyName, 1, "Foo")]
		[TestCase ("$(Foo) == 'a", TriggerState.Value, 1, "Foo")]
		[TestCase ("'$(Foo)' == 'a", TriggerState.Value, 1, "Foo")]
		[TestCase ("'$(Foo)|$(Bar)' == 'a", TriggerState.Value, 1, "Foo", "Bar")]
		[TestCase ("$(Foo) == 'a'", TriggerState.None, 0)]
		[TestCase ("$(Foo) == 'a' And $(Bar) >= '", TriggerState.Value, 0, "Bar")]
		public void TestConditionTriggering (params object[] args)
		{
			string expr = (string)args[0];
			var expectedState = (TriggerState)args[1];
			int expectedLength = (int)args[2];
			var expectedComparands = args.Skip (3).Cast<string> ().ToList ();

			var state = GetTriggerState (
				expr, '\0', true,
				out int triggerLength, out _, out _,
				out IReadOnlyList<ExpressionNode> comparandVariables
			);

			Assert.AreEqual (expectedState, state);
			Assert.AreEqual (expectedLength, triggerLength);
			Assert.AreEqual (expectedComparands.Count, comparandVariables?.Count ?? 0);
			for (int i = 0; i < expectedComparands.Count; i++) {
				Assert.AreEqual (expectedComparands[i], ((ExpressionProperty)comparandVariables[i]).Name);
			}
		}
	}
}
