#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class ExpeditionCardFace089ImportTests
    {
        private const string AssetRoot089 =
            "Assets/Resources/SecondDimension/Art/Board086/CardFaces/";
        private const string ResourceRoot089 =
            "SecondDimension/Art/Board086/CardFaces/";

        [TestCase("STORY")]
        [TestCase("CHEST")]
        [TestCase("BUFF")]
        [TestCase("HAZARD")]
        [TestCase("CHANCE")]
        [TestCase("RECRUIT")]
        [TestCase("CAMP")]
        [TestCase("BATTLE")]
        [TestCase("OBJECTIVE")]
        public void CardFaceIsAResourceLoadableSingleSprite089(string cardKey089)
        {
            var fileName089 = "CARD_FACE_" + cardKey089 + "_089";
            var assetPath089 = AssetRoot089 + fileName089 + ".png";
            var importer089 = AssetImporter.GetAtPath(assetPath089) as TextureImporter;
            var sprite089 = Resources.Load<Sprite>(ResourceRoot089 + fileName089);

            Assert.That(importer089, Is.Not.Null, assetPath089 + " has no texture importer.");
            Assert.That(sprite089, Is.Not.Null, assetPath089 + " is not Resources-loadable as a Sprite.");
            Assert.That(sprite089.texture.width, Is.GreaterThanOrEqualTo(1600));
            Assert.That(sprite089.texture.height, Is.GreaterThanOrEqualTo(900));
            Assert.That(importer089.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer089.spriteImportMode, Is.EqualTo(SpriteImportMode.Single));
            Assert.That(importer089.alphaIsTransparency, Is.True);
            Assert.That(importer089.mipmapEnabled, Is.False);
            Assert.That(importer089.streamingMipmaps, Is.False);
            Assert.That(importer089.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer089.filterMode, Is.EqualTo(FilterMode.Bilinear));
            Assert.That(importer089.textureCompression,
                Is.EqualTo(TextureImporterCompression.CompressedHQ));
            Assert.That(importer089.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer089.maxTextureSize, Is.EqualTo(2048));
            Assert.That(importer089.isReadable, Is.False);
        }
    }
}
#endif
