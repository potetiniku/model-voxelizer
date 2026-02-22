using Cysharp.Threading.Tasks;
using System.IO;
using System.Linq;
using UnityEngine;

public class AnimationPointCloudExporter : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer[] voxelizedMeshes;
    [SerializeField] private GameObject animationRoot;
    [SerializeField] private AnimationClip clip;
    [SerializeField] private float framesPerSecond = 60f;

    /// <summary>
    /// 1mあたりのボクセル数。小さいほど細部まで表現できるようになる。
    /// </summary>
    [SerializeField]
    private float voxelSize = 0.01f;

    private void Start()
    {
        ExportAsync().Forget();
    }

    public async UniTask ExportAsync()
    {
        float frameTime = 1f / framesPerSecond;

        for (float time = 0f; time < clip.length; time += frameTime)
        {
            clip.SampleAnimation(animationRoot, time);
            WriteToXyzFile($@"C:\ModelVoxelizer\animation_{time:F2}s.xyz");

            Debug.Log($"Sampling at time: {time:F2}s");
            await UniTask.WaitForSeconds(frameTime);
        }
    }

    private void WriteToXyzFile(string filePath)
    {
        MeshVoxelizer meshVoxelizer = new MeshVoxelizer();

        var xyzRecords = voxelizedMeshes
            .SelectMany(m => meshVoxelizer.Voxelize(m, voxelSize).SelectMany(t => t))
            .Select(p => new Point(new Vector3(-p.Position.x, p.Position.y, p.Position.z), p.Color))
            .Select(p => p.ToXyzRecord())
            .ToArray();

        File.WriteAllLines(filePath, xyzRecords);
        Debug.Log($"Saved {xyzRecords.Length} points.");
    }
}
