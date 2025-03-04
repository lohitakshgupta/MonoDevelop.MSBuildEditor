// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.MiniEditor;

using MonoDevelop.MSBuild.Editor;
using MonoDevelop.MSBuild.Editor.Roslyn;
using MonoDevelop.Xml.Tests.Completion;
using MonoDevelop.Xml.Tests.EditorTestHelpers;

using NUnit.Framework;

namespace MonoDevelop.MSBuild.Tests
{
	[TestFixture]
	public class MSBuildCompletionTests : CompletionTestBase
	{
		[OneTimeSetUp]
		public void LoadMSBuild () => MSBuildTestHelpers.RegisterMSBuildAssemblies ();

		protected override string ContentTypeName => MSBuildContentType.Name;

		protected override (EditorEnvironment, EditorCatalog) InitializeEnvironment () => TestEnvironment.EnsureInitialized ();

		[Test]
		public async Task TestElementCompletion ()
		{
			var result = await GetCompletionContext ("<Project>$");

			result.AssertNonEmpty ();

			result.AssertContains ("ItemGroup");
			result.AssertContains ("Choose");
			result.AssertContains ("Import");
		}


		[Test]
		public async Task ProjectCompletion ()
		{
			var result = await GetCompletionContext (@"<Project><$");

			//FIXME: add comment and closing tags
			result.AssertItemCount (9);

			result.AssertContains ("PropertyGroup");
			result.AssertContains ("Choose");
		}

		[Test]
		public async Task InferredItems ()
		{
			var result = await GetCompletionContext (@"
<Project><ItemGroup><Foo /><Bar /><$");

			// FIXME: add comment, cdata, closing tags for Project/ItemGroup
			result.AssertItemCount (2);

			result.AssertContains ("Foo");
			result.AssertContains ("Bar");
		}

		[Test]
		public async Task InferredMetadata ()
		{
			var result = await GetCompletionContext (@"
<Project><ItemGroup><Foo><Bar>a</Bar></Foo><Foo><$");

			// FIXME: add comment, cdata, closing tags for Project/ItemGroup/Foo
			result.AssertItemCount (1);

			result.AssertContains ("Bar");
		}

		[Test]
		public async Task InferredMetadataAttribute ()
		{
			var result = await GetCompletionContext (@"
<Project><ItemGroup><Foo Bar=""a"" /><Foo $");

			result.AssertItemCount (7);

			result.AssertContains ("Bar");
			result.AssertContains ("Include");
		}

		[Test]
		public async Task ProjectConfigurationConfigInference ()
		{
			var result = await GetCompletionContext (@"
<Project><ItemGroup>
<ProjectConfiguration Configuration='Foo' Platform='Bar' Include='Foo|Bar' />
<Baz Condition=""$(Configuration)=='^", caretMarker: '^');

			result.AssertItemCount (3);

			result.AssertContains ("Foo");
		}

		[Test]
		public async Task ProjectConfigurationPlatformInference ()
		{
			var result = await GetCompletionContext (@"
<Project><ItemGroup>
<ProjectConfiguration Configuration='Foo' Platform='Bar' Include='Foo|Bar' />
<Baz Condition=""$(Platform)=='^", caretMarker: '^');

			result.AssertItemCount (3);

			result.AssertContains ("Bar");
		}

		[Test]
		public async Task ConfigurationsInference ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup><Configurations>Foo;Bar</Configurations></PropertyGroup>
<ItemGroup>
<Baz Condition=""$(Configuration)=='^", caretMarker: '^');

			result.AssertItemCount (4);

			result.AssertContains ("Foo");
			result.AssertContains ("Bar");
		}

		[Test]
		public async Task PlatformsInference ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup><Platforms>Foo;Bar</Platforms></PropertyGroup>
<ItemGroup>
<Baz Condition=""$(Platform)=='^", caretMarker: '^');

			result.AssertItemCount (4);

			result.AssertContains ("Foo");
			result.AssertContains ("Bar");
		}

		[Test]
		public async Task ConditionConfigurationInference ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup Condition=""$(Configuration)=='Foo'"" />
<ItemGroup>
<Baz Condition=""$(Configuration)=='^", caretMarker: '^');

			result.AssertItemCount (3);

			result.AssertContains ("Foo");
		}

		[Test]
		public async Task PlatformConfigurationInference ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup Condition=""$(Platform)=='Foo'"" />
<ItemGroup>
<Baz Condition=""$(Platform)=='^", caretMarker: '^');

			result.AssertItemCount (3);

			result.AssertContains ("Foo");
		}

		[Test]
		public async Task ConfigurationAndPlatformInference ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup Condition=""'$(Platform)|$(Configuration)'=='Foo|Bar'"" />
<ItemGroup>
<Baz Condition=""'$(Platform)|$(Configuration)'=='^", caretMarker: '^');

			result.AssertItemCount (4);

			result.AssertContains ("Foo");
			result.AssertContains ("Bar");
		}

		[Test]
		public async Task IntrinsicStaticPropertyFunctionCompletion ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$([MSBuild]::^", caretMarker: '^');

			result.AssertItemCount (32);

			result.AssertContains ("GetDirectoryNameOfFileAbove");
		}

		[Test]
		public async Task StaticPropertyFunctionCompletion ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$([System.String]::^", caretMarker: '^');

			result.AssertNonEmpty ();

			result.AssertContains ("new");
			result.AssertContains ("Join");
			result.AssertDoesNotContain ("ToLower");
		}

		[Test]
		public async Task PropertyStringFunctionCompletion ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$(Foo.^", caretMarker: '^');

			result.AssertNonEmpty ();

			//string functions
			result.AssertContains ("ToLower");
			//properties can be accessed with the getter method
			result.AssertContains ("get_Length");
			//.net properties are allowed for properties
			result.AssertContains ("Length");
			//indexers should be filtered out
			result.AssertDoesNotContain ("this[]");
		}

		[Test]
		public async Task ItemFunctionCompletion ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>@(Foo->^", caretMarker: '^');

			result.AssertNonEmpty ();

			//intrinsic functions
			result.AssertContains ("DistinctWithCase");
			result.AssertContains ("Metadata");
			//string functions
			result.AssertContains ("ToLower");
			//properties can be accessed with the getter method
			result.AssertContains ("get_Length");
			//.net properties are not allowed for items
			result.AssertDoesNotContain ("Length");
			//indexers should be filtered out
			result.AssertDoesNotContain ("this[]");
		}

		[Test]
		public async Task PropertyFunctionClassNames ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$([^", caretMarker: '^');

			result.AssertNonEmpty ();
			result.AssertContains ("MSBuild");
			result.AssertContains ("System.String");
		}

		[Test]
		public async Task PropertyFunctionChaining ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$([System.DateTime]::Now.^", caretMarker: '^');

			result.AssertNonEmpty ();
			result.AssertContains ("AddDays");
		}

		[Test]
		public async Task IndexerChaining ()
		{
			var result = await GetCompletionContext (@"
<Project>
<PropertyGroup>
<Foo>$(Foo[0].^", caretMarker: '^');

			result.AssertNonEmpty ();
			result.AssertContains ("CompareTo");
			result.AssertDoesNotContain ("Substring");
		}
	}

	[Export (typeof (IRoslynCompilationProvider))]
	class TestCompilationProvider : IRoslynCompilationProvider
	{
		public MetadataReference CreateReference (string assemblyPath)
			=> MetadataReference.CreateFromFile (assemblyPath);
	}
}
