using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SpatialNavigationTests
{
    private readonly List<GameObject> objects = new List<GameObject>();
    private SpatialNavigationMesh Volume(int mask)
    {
        var go = new GameObject("Test volume");
        objects.Add(go);
        var volume = go.AddComponent<SpatialNavigationMesh>();
        volume.Configure(16f, 3, mask);
        return volume;
    }
    private void Obstacle(Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        objects.Add(go);
        go.layer = 30;
        go.transform.localScale = size;
        Physics.SyncTransforms();
    }
    [TearDown] public void Cleanup()
    {
        foreach (var go in objects) { Object.DestroyImmediate(go); }
        objects.Clear();
    }
    [Test] public void EmptyVolumeReturnsOneClearCell()
    {
        var path = Volume(0).CreatePath(Vector3.left, Vector3.right);
        Assert.That(path.Count, Is.EqualTo(1));
        Assert.That(path[0].isClear, Is.True);
    }
    [Test] public void FullyBlockedVolumeReturnsEmpty()
    {
        Obstacle(Vector3.one * 32f);
        Assert.That(Volume(1 << 30).CreatePath(Vector3.left, Vector3.right), Is.Empty);
    }
    [Test] public void WallSeparatingVolumeReturnsEmpty()
    {
        Obstacle(new Vector3(2f, 32f, 32f));
        Assert.That(Volume(1 << 30).CreatePath(Vector3.left * 6f, Vector3.right * 6f), Is.Empty);
    }
    [Test] public void RoutesAroundCentralObstacle()
    {
        Obstacle(Vector3.one * 4f);
        var path = Volume(1 << 30).CreatePath(Vector3.left * 6f, Vector3.right * 6f);
        Assert.That(path.Count, Is.GreaterThan(1));
        foreach (var cell in path) { Assert.That(cell.isClear, Is.True); }
    }
    [Test] public void RebuildReflectsChangedGeometry()
    {
        var volume = Volume(1 << 30);
        volume.Build();
        Obstacle(Vector3.one * 32f);
        volume.Build();
        Assert.That(volume.CreatePath(Vector3.left, Vector3.right), Is.Empty);
    }
}
