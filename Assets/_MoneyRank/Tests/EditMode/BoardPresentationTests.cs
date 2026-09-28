using MoneyRank.Content;
using MoneyRank.Presentation.Board;
using NUnit.Framework;
using UnityEngine;

namespace MoneyRank.Tests.EditMode
{
    public sealed class BoardPresentationTests
    {
        [Test]
        public void TemporaryAssetConvertsToDomainDefinition()
        {
            var asset = ScriptableObject.CreateInstance<BoardDefinitionAsset>();

            try
            {
                var board = asset.ToDomain();

                Assert.That(board.Id, Is.EqualTo("vertical-slice-board"));
                Assert.That(board.SpaceCount, Is.EqualTo(20));
                Assert.That(board.GetSpace(0).TypeId, Is.EqualTo("start"));
                Assert.That(board.GetSpace(1).CategoryId, Is.EqualTo("expense"));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void LayoutBuildsOneAnchorPerConfiguredSpace()
        {
            var asset = ScriptableObject.CreateInstance<BoardDefinitionAsset>();
            var root = new GameObject("Board Test Root");

            try
            {
                var view = root.AddComponent<BoardLayoutView>();
                view.Initialize(asset);

                Assert.That(view.SpaceAnchors, Has.Count.EqualTo(asset.Spaces.Count));
                Assert.That(view.SpaceAnchors[0].name, Does.Contain("start"));
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(asset);
            }
        }
    }
}
