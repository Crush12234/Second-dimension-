using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using SecondDimension.Presentation.Creator028;
namespace SecondDimension.Tests.PlayMode
{
 public sealed class CreatorCodesRooms028PlayModeTests
 {
  [UnityTest] public IEnumerator CreatorRegistryLoadsInPlayerContext(){var r=CreatorRegistry028.Load();Assert.That(r.CodeCount,Is.EqualTo(300));Assert.That(r.RoomCount,Is.EqualTo(133));yield return null;}
 }
}
