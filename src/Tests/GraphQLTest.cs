﻿using GraphQLClient;
using NUnit.Framework;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Tests
{
	[TestFixture]
	[Explicit("External API call")]
	internal class GraphQLTest
	{
		private static string fingerprint = Guid.NewGuid().ToString();

		[Test]
		public async Task GetRecipesForClientByShortcutAsync_should_return_all_snippets()
		{
			// arrange
			var client = new CodigaClient(fingerprint);

			// act
			var snippets = await client.GetRecipesForClientByShortcutAsync("Csharp");

			// assert
			Assert.NotNull(snippets);
			Assert.IsNotEmpty(snippets);
		}

		[Test]
		public async Task GetRecipesForClientByShortcutLastTimestamp_should_return_last_timestamp()
		{
			// arrange
			var client = new CodigaClient(fingerprint);

			// act
			var ts = await client.GetRecipesForClientByShortcutLastTimestampAsync("Csharp");

			// assert
			Assert.NotNull(ts);
		}

		[Test]
		public async Task RecordRecipeUse_should_record_access()
		{
			// arrange
			var client = new CodigaClient(fingerprint);

			// act
			var result = await client.RecordRecipeUseAsync(9260);

			// assert
			Assert.That(result, Is.EqualTo("ok"));
		}

		[Test]
		public async Task GetRecipesForClientSemantic_should_return_snippets()
		{
			// arrange
			var client = new CodigaClient(fingerprint);
			var languages = new ReadOnlyCollection<string>(new[] { "Csharp" });
			var watch = new Stopwatch();
			watch.Start();
			// act
			var result = await client.GetRecipesForClientSemanticAsync("add test", languages, true, 10, 0);

			// assert
			var time = watch.Elapsed;
			TestContext.WriteLine($"Took {time.Milliseconds}ms");
			Assert.NotNull(result);
			Assert.IsNotEmpty(result);
		}

		[Test]
		public async Task GetUser_should_return_user()
		{
			// arrange
			var token = Environment.GetEnvironmentVariable("XApiToken");
			var client = new CodigaClient(token, fingerprint);

			// act
			var result = await client.GetUserAsync();

			// assert
			Assert.NotNull(result);
		}
	}
}