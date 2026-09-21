using System.Reflection;
using NUnit.Framework;
using SecondDimension.Presentation;
using UnityEngine;

namespace SecondDimension.Tests.EditMode
{
    public sealed class EnemyArt700Battle3DGroundContact090Tests
    {
        private Texture2D _texture090;
        private Sprite _sprite090;
        private GameObject _actor090;

        [TearDown]
        public void TearDown090()
        {
            if (_actor090 != null) Object.DestroyImmediate(_actor090);
            if (_sprite090 != null) Object.DestroyImmediate(_sprite090);
            if (_texture090 != null) Object.DestroyImmediate(_texture090);
        }

        [Test]
        public void LowAuthoredPivotPlacesEnemyArtBottomOnThe3dGround090()
        {
            const float renderedHeight090 = 6.4f;
            _texture090 = new Texture2D(768, 1024, TextureFormat.RGBA32, false);
            _sprite090 = Sprite.Create(
                _texture090,
                new Rect(0f, 0f, 768f, 1024f),
                new Vector2(0.5f, 100f / 1024f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _actor090 = new GameObject("EnemyArt700 Ground Contact Probe 090");

            var method090 = typeof(M2Battle3DWorld).GetMethod(
                "FitGroundedActorSprite090",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method090, Is.Not.Null);
            method090.Invoke(
                null,
                new object[] { _actor090.transform, _sprite090, renderedHeight090 });

            var scaledBottom090 = _actor090.transform.localPosition.y +
                                  _sprite090.bounds.min.y *
                                  _actor090.transform.localScale.y;
            var scaledHeight090 = _sprite090.bounds.size.y *
                                  _actor090.transform.localScale.y;
            Assert.That(scaledBottom090, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(scaledHeight090,
                Is.EqualTo(renderedHeight090).Within(0.0001f));
            Assert.That(_actor090.transform.localPosition.y,
                Is.LessThan(renderedHeight090 * 0.2f),
                "The low EnemyArt pivot must not be positioned as if it were centered.");
        }

        [Test]
        public void CenteredFallbackRetainsItsExistingHalfHeightPlacement090()
        {
            const float renderedHeight090 = 6.4f;
            _texture090 = new Texture2D(768, 1024, TextureFormat.RGBA32, false);
            _sprite090 = Sprite.Create(
                _texture090,
                new Rect(0f, 0f, 768f, 1024f),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect);
            _actor090 = new GameObject("Centered Fallback Ground Contact Probe 090");

            var method090 = typeof(M2Battle3DWorld).GetMethod(
                "FitGroundedActorSprite090",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method090, Is.Not.Null);
            method090.Invoke(
                null,
                new object[] { _actor090.transform, _sprite090, renderedHeight090 });

            var scaledBottom090 = _actor090.transform.localPosition.y +
                                  _sprite090.bounds.min.y *
                                  _actor090.transform.localScale.y;
            Assert.That(scaledBottom090, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(_actor090.transform.localPosition.y,
                Is.EqualTo(renderedHeight090 * 0.5f).Within(0.0001f));
        }
    }
}
