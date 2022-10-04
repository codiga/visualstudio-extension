using GraphQLClient;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake;
using System.Collections.ObjectModel;
using System.Net.Http.Headers;
using System.Text;

namespace Tests
{
	[TestFixture]
	[Explicit("Testing the external API should not be part of automated testing of this project")]
	internal class CodigaClientTest
	{
		private ICodigaGraphQLClient mClient;

		[SetUp]
		public void SetUp()
		{
			var serviceCollection = new ServiceCollection();
			var token = Environment.GetEnvironmentVariable("XApiToken");

			serviceCollection.AddCodigaGraphQLClient(StrawberryShake.ExecutionStrategy.CacheFirst)
				.ConfigureHttpClient(c =>
				{
					c.BaseAddress = new Uri("https://api.codiga.io/graphql");
					c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("X-Api-Token", token);
				});

			var serviceProvider = serviceCollection.BuildServiceProvider();
			mClient = serviceProvider.GetRequiredService<ICodigaGraphQLClient>();
		}

		[Test]
		public async Task GetRecipesForClientByShortcutTest()
		{
			// act
			var result = await mClient.GetRecipesForClientByShortcut.ExecuteAsync(
				fingerprint: null,
				filename: "",
				term: "",
				dependencies: new ReadOnlyCollection<string>(new[] { "" }),
				parameters: null,
				language: LanguageEnumeration.Csharp,
				onlyPublic: true,
				onlyPrivate: false,
				onlySubscribed: false);

			// assert
			Assert.False(result.IsErrorResult(), $"{result.Errors.FirstOrDefault()?.Message} " +
				$"{result.Errors.FirstOrDefault()?.Locations?.First().Line}:" +
				$"{result.Errors.FirstOrDefault()?.Locations?.First().Column}");

			var data = result.Data.GetRecipesForClientByShortcut.First();
			var decodedBytes = Convert.FromBase64String(data.Code);
			var decodedText = Encoding.UTF8.GetString(decodedBytes);
		}

		[Test]
		public async Task GetRecipesForClientSemanticTest()
		{
			// act
			var result = await mClient.GetRecipesForClientSemantic.ExecuteAsync(
				term: null,
				onlyPublic: true,
				onlyPrivate: false,
				onlySubscribed: false,
				filename: null,
				dependencies: new ReadOnlyCollection<string>(new[] { "" }),
				parameters: null,
				languages: new ReadOnlyCollection<LanguageEnumeration>(new[] { LanguageEnumeration.Csharp }),
				howmany: 100,
				skip: 0);

			// assert
			Assert.False(result.IsErrorResult(), result.Errors.FirstOrDefault()?.Message);

			var data = result.Data.AssistantRecipesSemanticSearch.First();
			var decodedBytes = Convert.FromBase64String(data.Code);
			var decodedText = Encoding.UTF8.GetString(decodedBytes);
		}

		[Test]
		public async Task GetRecipesForClientTest()
		{
			// act
			var result = await mClient.GetRecipesForClient.ExecuteAsync(
				fingerprint: "85430958324ß05832ß048",
				filename: null,
				keywords: new ReadOnlyCollection<string>(new[] { "read" }),
				dependencies: new ReadOnlyCollection<string>(new[] { "" }),
				parameters: null,
				language: LanguageEnumeration.Csharp);

			// assert
			Assert.False(result.IsErrorResult(), result.Errors.FirstOrDefault()?.Message);

			var data = result.Data.GetRecipesForClient.First();
			var decodedBytes = Convert.FromBase64String(data.Code);
			var decodedText = Encoding.UTF8.GetString(decodedBytes);
		}

		[Test]
		public async Task GetRecipesForClientByShortcutLastTimestampTest()
		{
			// act
			var result = await mClient.GetRecipesForClientByShortcutLastTimestamp.ExecuteAsync(
				fingerprint: null,
				dependencies: new ReadOnlyCollection<string>(new[] { "" }),
				language: LanguageEnumeration.Csharp);

			// assert
			Assert.False(result.IsErrorResult(), result.Errors.FirstOrDefault()?.Message);

			var data = result.Data.GetRecipesForClientByShortcutLastTimestamp;
		}

		[Test]
		public async Task GetUserTest()
		{
			// act
			var result = await mClient.GetUser.ExecuteAsync();

			// assert
			Assert.False(result.IsErrorResult(), result.Errors.FirstOrDefault()?.Message);
			var data = result.Data.User.Username;
		}
	}
}