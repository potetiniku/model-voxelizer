using System.Collections.Generic;
using UnityEngine;

public class MeshVoxelizer
{
    /// <summary>
    /// メッシュを格子状の点群 (ボクセル) に変換する
    /// </summary>
    /// <param name="voxelizedMesh">変換するメッシュ</param>
    /// <param name="voxelSize">1mあたりのボクセル数。小さいほど細部まで表現できるようになる。</param>
    /// <returns>変換された点群。サブメッシュ単位で列挙される。</returns>
    public IEnumerable<IEnumerable<Point>> Voxelize(SkinnedMeshRenderer voxelizedMesh, float voxelSize)
    {
        Mesh currentMesh = new();
        voxelizedMesh.BakeMesh(currentMesh);

        var vertices = currentMesh.vertices;
        var uvs = currentMesh.uv;

        // 頂点の座標をワールド座標へ変換
        for (int i = 0; i < vertices.Length; i++)
            vertices[i] = voxelizedMesh.transform.TransformPoint(vertices[i]);

        // サブメッシュ単位で処理
        for (int sub = 0; sub < currentMesh.subMeshCount; sub++)
        {
            // マテリアル数と一致しない場合はスキップ
            if (sub >= voxelizedMesh.sharedMaterials.Length) continue;

            var material = voxelizedMesh.sharedMaterials[sub];
            var texture = material.mainTexture as Texture2D;

            if (texture == null) continue;

            // そのサブメッシュに属する三角形インデックス取得
            int[] triangles = currentMesh.GetTriangles(sub);

            // 三角形ごとに均一サンプリング
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];

                yield return VoxelizeTriangle(
                    vertices[i0], vertices[i1], vertices[i2],
                    uvs[i0], uvs[i1], uvs[i2],
                    texture,
                    voxelSize);
            }
        }
    }

    /// <summary>
    /// 三角形を均一にサンプリングし、
    /// アルファ50%以上の点のみボクセル登録する
    /// </summary>
    private IEnumerable<Point> VoxelizeTriangle(
        Vector3 v0, Vector3 v1, Vector3 v2,
        Vector2 uv0, Vector2 uv1, Vector2 uv2,
        Texture2D texture,
        float voxelSize)
    {
        float maxEdgeLength = Mathf.Max(
            Vector3.Distance(v0, v1),
            Vector3.Distance(v1, v2),
            Vector3.Distance(v2, v0));

        // ボクセルサイズ基準で分割数を決定
        int steps = Mathf.CeilToInt(maxEdgeLength / voxelSize);

        if (steps < 1)
            steps = 1;

        // barycentric格子サンプリング
        for (int i = 0; i <= steps; i++)
            for (int j = 0; j <= steps - i; j++)
            {
                float u = i / (float)steps;
                float v = j / (float)steps;
                float w = 1f - u - v;

                // 位置を補間
                Vector3 p =
                    u * v0 +
                    v * v1 +
                    w * v2;

                // UVも同じ重みで補間
                Vector2 uv =
                    u * uv0 +
                    v * uv1 +
                    w * uv2;

                var color = texture.GetPixelBilinear(uv.x, uv.y);

                // 透過度の高い点は出力しない
                if (color.a < 0.5f) continue;

                yield return new Point(p, color);
            }
    }
}
