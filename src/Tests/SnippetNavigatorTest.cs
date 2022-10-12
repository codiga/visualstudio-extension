using Extension.InlineCompletion;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
	[TestFixture]
	internal class ListNavigatorTest
	{
		[Test]
		public void Next_should_cycle_through_snippets()
		{
			// arrange
			var nav = new ListNavigator<string>(new[] {"snippet1", "snippet2", "snippet3"});

			// act & assert
			var next = nav.First();
			Assert.That(next, Is.EqualTo("snippet1"));

			next = nav.Next();
			Assert.That(next, Is.EqualTo("snippet2"));

			next = nav.Next();
			Assert.That(next, Is.EqualTo("snippet3"));

			next = nav.Next();
			Assert.That(next, Is.EqualTo("snippet1"));
		}

		[Test]
		public void Previous_should_cycle_through_snippets()
		{
			// arrange
			var nav = new ListNavigator<string>(new[] { "snippet1", "snippet2", "snippet3" });

			// act & assert
			var previous = nav.First();
			Assert.That(previous, Is.EqualTo("snippet1"));

			previous = nav.Previous();
			Assert.That(previous, Is.EqualTo("snippet3"));

			previous = nav.Previous();
			Assert.That(previous, Is.EqualTo("snippet2"));

			previous = nav.Previous();
			Assert.That(previous, Is.EqualTo("snippet1"));
		}
	}
}
