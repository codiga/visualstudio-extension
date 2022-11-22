using System.Collections.Generic;
using System.IO;
using System.Text;
using EnvDTE;
using Extension.Caching;
using Extension.Rosie;
using GraphQLClient;
using Moq;
using NUnit.Framework;

namespace Tests.Rosie
{
    /// <summary>
    /// Unit test for <see cref="RosieRulesCache"/>.
    /// </summary>
    /// <seealso cref="https://www.codeproject.com/Articles/5286617/Moq-and-out-Parameter"/>
    /// <seealso cref="https://stackoverflow.com/questions/1068095/assigning-out-ref-parameters-in-moq"/>
    [TestFixture]
    internal class RosieRulesCacheTest
    {
        private Mock<Solution> _solution;
        private string _solutionDirPath;
        private string _codigaConfigFile;

        [SetUp]
        public void Setup()
        {
            _solutionDirPath = Path.GetTempPath();
            _codigaConfigFile = $"{_solutionDirPath}codiga.yml";

            _solution = new Mock<Solution>();
            _solution.Setup(s => s.FullName).Returns(_solutionDirPath);
        }

        [Test]
        public void HandleCacheUpdate_should____()
        {
            InitCodigaConfig(@"
rulesets:
  - pm-python-ruleset
");

            var codigaClient = new Mock<ICodigaClient>();
            codigaClient.Setup(c => c.GetRulesetsLastUpdatedTimestampAsync(It.IsAny<IReadOnlyCollection<string>>()))
                .ReturnsAsync(10);

            RosieRulesCache.Initialize(_solution.Object, GetClientProvider(codigaClient));
            RosieRulesCache.Instance.HandleCacheUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            RosieRulesCache.Dispose();
            File.Delete(_codigaConfigFile);
        }

        private void InitCodigaConfig(string rawConfig)
        {
            using var fs = File.Create(_codigaConfigFile);
            var info = new UTF8Encoding(true).GetBytes(rawConfig);
            fs.Write(info, 0, info.Length);
        }

        private static ICodigaClientProvider GetClientProvider(Mock<ICodigaClient> codigaClient)
        {
            var clientProvider = new Mock<ICodigaClientProvider>();
            clientProvider.Setup(p => p.GetClient()).Returns(codigaClient.Object);

            ICodigaClient? client;
            clientProvider
                .Setup(p => p.TryGetClient(out client))
                .Callback(new TryGetClient((out ICodigaClient c) => { c = codigaClient.Object; }))
                .Returns(true);

            return clientProvider.Object;
        }
    }

    public delegate void TryGetClient(out ICodigaClient client);
}