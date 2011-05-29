﻿using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.ServiceHost.Tests.Formats
{
	[TestFixture]
	public class TextBlockTests
	{
		string dynamicListPagePath;
		string dynamicListPageContent;

		[TestFixtureSetUp]
		public void TestFixtureSetUp()
		{
			dynamicListPagePath = "~/AppData/Template/DynamicListTpl.md".MapAbsolutePath();
			dynamicListPageContent = File.ReadAllText(dynamicListPagePath);
		}

		[Test]
		public void Does_replace_foreach_statements_with_expr_placeholders()
		{
			var content = (string)dynamicListPageContent.Clone();

			var expected = content.ReplaceForeach("@^1");;

			var statements = new List<StatementExprBlock>();
			var parsedContent = StatementExprBlock.Extract(content, statements);

			Console.WriteLine(parsedContent);

			Assert.That(parsedContent, Is.EqualTo(expected));
			Assert.That(statements.Count, Is.EqualTo(1));
			Assert.That(statements[0].Condition, Is.EqualTo("var link in Model.Links"));
			Assert.That(statements[0].Statement, Is.EqualTo("  - @link.Name - @link.Href\r\n"));
		}

		[Test]
		public void Does_replace_multiple_statements_with_expr_placeholders()
		{
			const string template = @"
## Statement 1

@if (Model.IsValid) {
### This is valid
}

@foreach (var link in Model.Links) {
  - @link.Name - @link.Href
}

## Statement 2

@foreach (var text in Model.Texts) {
### @text.Name
@text.body
}

@if (!Model.IsValid) {
### This is not valid
}

# EOF";
			const string expected = @"
## Statement 1

@^1

@^2

## Statement 2

@^3

@^4

# EOF";
			var statements = new List<StatementExprBlock>();
			var content = StatementExprBlock.Extract(template, statements);

			Console.WriteLine(content);

			Assert.That(content, Is.EqualTo(expected));
			Assert.That(statements.Count, Is.EqualTo(4));
			Assert.That(statements[0].Condition, Is.EqualTo("Model.IsValid"));
			Assert.That(statements[0].Statement, Is.EqualTo("### This is valid\r\n"));
			Assert.That(statements[1].Condition, Is.EqualTo("var link in Model.Links"));
			Assert.That(statements[1].Statement, Is.EqualTo("  - @link.Name - @link.Href\r\n"));
			Assert.That(statements[2].Condition, Is.EqualTo("var text in Model.Texts"));
			Assert.That(statements[2].Statement, Is.EqualTo("### @text.Name\r\n@text.body\r\n"));
			Assert.That(statements[3].Condition, Is.EqualTo("!Model.IsValid"));
			Assert.That(statements[3].Statement, Is.EqualTo("### This is not valid\r\n"));
		}

		[Test]
		public void Does_parse_parens_free_statements()
		{
			const string template = @"
## Statement 1

@if Model.IsValid {
### This is valid
}

@foreach var link in Model.Links {
  - @link.Name - @link.Href
}

## Statement 2

@foreach text in Model.Texts {
### @text.Name
@text.body
}

@if !Model.IsValid{
### This is not valid
}

# EOF";
			const string expected = @"
## Statement 1

@^1

@^2

## Statement 2

@^3

@^4

# EOF";

			var statements = new List<StatementExprBlock>();
			var content = StatementExprBlock.Extract(template, statements);

			Console.WriteLine(content);

			Assert.That(content, Is.EqualTo(expected));
			Assert.That(statements.Count, Is.EqualTo(4));

			var stat1 = (IfStatementExprBlock)statements[0];
			Assert.That(stat1.Condition, Is.EqualTo("Model.IsValid"));
			Assert.That(stat1.Statement, Is.EqualTo("### This is valid\r\n"));

			var stat2 = (ForEachStatementExprBlock)statements[1];
			Assert.That(stat2.Condition, Is.EqualTo("var link in Model.Links"));
			Assert.That(stat2.Statement, Is.EqualTo("  - @link.Name - @link.Href\r\n"));
			Assert.That(stat2.EnumeratorName, Is.EqualTo("link"));
			Assert.That(stat2.MemberExpr, Is.EqualTo("Model.Links"));

			var stat3 = (ForEachStatementExprBlock)statements[2];
			Assert.That(stat3.Condition, Is.EqualTo("text in Model.Texts"));
			Assert.That(stat3.Statement, Is.EqualTo("### @text.Name\r\n@text.body\r\n"));
			Assert.That(stat3.EnumeratorName, Is.EqualTo("text"));
			Assert.That(stat3.MemberExpr, Is.EqualTo("Model.Texts"));

			var stat4 = (IfStatementExprBlock)statements[3];
			Assert.That(stat4.Condition, Is.EqualTo("!Model.IsValid"));
			Assert.That(stat4.Statement, Is.EqualTo("### This is not valid\r\n"));
		}
	}
}