using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 평면(Plane)을 기준으로 메시를 두 조각으로 분할하는 정적 유틸리티.
/// SlicedMesh 결과에는 양쪽 메시와 단면 캡이 포함된다.
/// </summary>
public static class MeshSlicer
{
    /// <summary>
    /// 슬라이싱 결과. upperMesh는 평면 법선 방향, lowerMesh는 반대 방향.
    /// 단면(캡)은 각 메시의 submesh 1에 포함된다.
    /// </summary>
    public class SlicedMesh
    {
        public Mesh upperMesh;
        public Mesh lowerMesh;
    }

    private struct MeshData
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<Vector2> uvs;
        public List<int> triangles;
        public List<int> capTriangles;

        public static MeshData Create()
        {
            return new MeshData
            {
                vertices = new List<Vector3>(256),
                normals = new List<Vector3>(256),
                uvs = new List<Vector2>(256),
                triangles = new List<int>(256),
                capTriangles = new List<int>(64)
            };
        }

        public int AddVertex(Vector3 vertex, Vector3 normal, Vector2 uv)
        {
            int index = vertices.Count;
            vertices.Add(vertex);
            normals.Add(normal);
            uvs.Add(uv);
            return index;
        }

        public Mesh ToMesh()
        {
            var mesh = new Mesh();
            mesh.subMeshCount = 2;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetTriangles(capTriangles, 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>
    /// 메시를 평면 기준으로 두 조각으로 분할한다.
    /// 분할 불가능한 경우(모든 정점이 한쪽에 있는 경우) null을 반환한다.
    /// [필수] slicePlane은 메시의 로컬 좌표계 기준이어야 한다.
    /// </summary>
    public static SlicedMesh Slice(Mesh sourceMesh, Plane slicePlane)
    {
        if (sourceMesh == null) return null;

        Vector3[] sourceVertices = sourceMesh.vertices;
        Vector3[] sourceNormals = sourceMesh.normals;
        Vector2[] sourceUvs = sourceMesh.uv;
        int[] sourceTriangles = sourceMesh.triangles;

        if (sourceNormals.Length == 0) sourceMesh.RecalculateNormals();
        if (sourceNormals.Length == 0) sourceNormals = sourceMesh.normals;

        // 각 정점이 평면의 어느 쪽에 있는지 분류
        bool[] isAbove = new bool[sourceVertices.Length];
        bool hasAbove = false;
        bool hasBelow = false;

        for (int i = 0; i < sourceVertices.Length; i++)
        {
            isAbove[i] = slicePlane.GetSide(sourceVertices[i]);
            if (isAbove[i]) hasAbove = true;
            else hasBelow = true;
        }

        // 모든 정점이 한쪽에 있으면 분할 불가
        if (!hasAbove || !hasBelow) return null;

        MeshData upper = MeshData.Create();
        MeshData lower = MeshData.Create();

        // 단면 캡 생성을 위한 교차 에지 수집
        List<Vector3> intersectionPoints = new List<Vector3>(32);

        // 삼각형 단위로 처리
        for (int i = 0; i < sourceTriangles.Length; i += 3)
        {
            int index0 = sourceTriangles[i];
            int index1 = sourceTriangles[i + 1];
            int index2 = sourceTriangles[i + 2];

            bool above0 = isAbove[index0];
            bool above1 = isAbove[index1];
            bool above2 = isAbove[index2];

            if (above0 == above1 && above1 == above2)
            {
                // 삼각형 전체가 한쪽에 있음
                MeshData target = above0 ? upper : lower;
                int vertexIndex0 = target.AddVertex(sourceVertices[index0], sourceNormals[index0], GetUV(sourceUvs, index0));
                int vertexIndex1 = target.AddVertex(sourceVertices[index1], sourceNormals[index1], GetUV(sourceUvs, index1));
                int vertexIndex2 = target.AddVertex(sourceVertices[index2], sourceNormals[index2], GetUV(sourceUvs, index2));
                target.triangles.Add(vertexIndex0);
                target.triangles.Add(vertexIndex1);
                target.triangles.Add(vertexIndex2);
            }
            else
            {
                // 삼각형이 평면과 교차함 — 분할 필요
                SplitTriangle(
                    sourceVertices, sourceNormals, sourceUvs,
                    index0, index1, index2,
                    above0, above1, above2,
                    slicePlane, upper, lower, intersectionPoints
                );
            }
        }

        // 단면 캡 생성
        if (intersectionPoints.Count >= 3)
        {
            GenerateCap(intersectionPoints, slicePlane.normal, upper, lower);
        }

        return new SlicedMesh
        {
            upperMesh = upper.ToMesh(),
            lowerMesh = lower.ToMesh()
        };
    }

    private static Vector2 GetUV(Vector2[] uvs, int index)
    {
        return (uvs != null && index < uvs.Length) ? uvs[index] : Vector2.zero;
    }

    private static void SplitTriangle(
        Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
        int index0, int index1, int index2,
        bool above0, bool above1, bool above2,
        Plane plane, MeshData upper, MeshData lower,
        List<Vector3> intersectionPoints)
    {
        // 홀로 한쪽에 있는 정점을 찾는다 (1개 vs 2개 분리)
        int soloIndex, pairIndex1, pairIndex2;
        bool soloIsAbove;

        if (above0 != above1 && above0 != above2)
        {
            soloIndex = index0; pairIndex1 = index1; pairIndex2 = index2;
            soloIsAbove = above0;
        }
        else if (above1 != above0 && above1 != above2)
        {
            soloIndex = index1; pairIndex1 = index0; pairIndex2 = index2;
            soloIsAbove = above1;
        }
        else
        {
            soloIndex = index2; pairIndex1 = index0; pairIndex2 = index1;
            soloIsAbove = above2;
        }

        // solo → pair1 에지와 평면의 교차점
        float t1 = IntersectEdge(vertices[soloIndex], vertices[pairIndex1], plane);
        Vector3 intersect1 = Vector3.Lerp(vertices[soloIndex], vertices[pairIndex1], t1);
        Vector3 normal1 = Vector3.Lerp(normals[soloIndex], normals[pairIndex1], t1).normalized;
        Vector2 uv1 = Vector2.Lerp(GetUV(uvs, soloIndex), GetUV(uvs, pairIndex1), t1);

        // solo → pair2 에지와 평면의 교차점
        float t2 = IntersectEdge(vertices[soloIndex], vertices[pairIndex2], plane);
        Vector3 intersect2 = Vector3.Lerp(vertices[soloIndex], vertices[pairIndex2], t2);
        Vector3 normal2 = Vector3.Lerp(normals[soloIndex], normals[pairIndex2], t2).normalized;
        Vector2 uv2 = Vector2.Lerp(GetUV(uvs, soloIndex), GetUV(uvs, pairIndex2), t2);

        intersectionPoints.Add(intersect1);
        intersectionPoints.Add(intersect2);

        // solo 쪽: 삼각형 1개 (solo, intersect1, intersect2)
        MeshData soloSide = soloIsAbove ? upper : lower;
        int soloVertexIndex = soloSide.AddVertex(vertices[soloIndex], normals[soloIndex], GetUV(uvs, soloIndex));
        int intersectVertexIndex1 = soloSide.AddVertex(intersect1, normal1, uv1);
        int intersectVertexIndex2 = soloSide.AddVertex(intersect2, normal2, uv2);
        soloSide.triangles.Add(soloVertexIndex);
        soloSide.triangles.Add(intersectVertexIndex1);
        soloSide.triangles.Add(intersectVertexIndex2);

        // pair 쪽: 삼각형 2개 (pair1, pair2, intersect1), (pair2, intersect2, intersect1)
        MeshData pairSide = soloIsAbove ? lower : upper;
        int pairVertexIndex1 = pairSide.AddVertex(vertices[pairIndex1], normals[pairIndex1], GetUV(uvs, pairIndex1));
        int pairVertexIndex2 = pairSide.AddVertex(vertices[pairIndex2], normals[pairIndex2], GetUV(uvs, pairIndex2));
        int pairIntersect1 = pairSide.AddVertex(intersect1, normal1, uv1);
        int pairIntersect2 = pairSide.AddVertex(intersect2, normal2, uv2);

        pairSide.triangles.Add(pairVertexIndex1);
        pairSide.triangles.Add(pairIntersect1);
        pairSide.triangles.Add(pairIntersect2);

        pairSide.triangles.Add(pairVertexIndex1);
        pairSide.triangles.Add(pairIntersect2);
        pairSide.triangles.Add(pairVertexIndex2);
    }

    private static float IntersectEdge(Vector3 from, Vector3 to, Plane plane)
    {
        float distanceFrom = plane.GetDistanceToPoint(from);
        float distanceTo = plane.GetDistanceToPoint(to);
        return distanceFrom / (distanceFrom - distanceTo);
    }

    private static void GenerateCap(List<Vector3> intersectionPoints, Vector3 planeNormal, MeshData upper, MeshData lower)
    {
        // 교차점들의 중심 계산
        Vector3 center = Vector3.zero;
        for (int i = 0; i < intersectionPoints.Count; i++)
        {
            center += intersectionPoints[i];
        }
        center /= intersectionPoints.Count;

        // 중복 제거 (근접한 점들 병합)
        List<Vector3> uniquePoints = new List<Vector3>(intersectionPoints.Count);
        for (int i = 0; i < intersectionPoints.Count; i++)
        {
            bool isDuplicate = false;
            for (int j = 0; j < uniquePoints.Count; j++)
            {
                if (Vector3.Distance(intersectionPoints[i], uniquePoints[j]) < 0.001f)
                {
                    isDuplicate = true;
                    break;
                }
            }
            if (!isDuplicate) uniquePoints.Add(intersectionPoints[i]);
        }

        if (uniquePoints.Count < 3) return;

        // 평면 위의 로컬 좌표축 구성
        Vector3 tangent = (uniquePoints[0] - center).normalized;
        Vector3 bitangent = Vector3.Cross(planeNormal, tangent).normalized;

        // 각도 기준으로 정렬 (fan triangulation을 위해)
        uniquePoints.Sort((pointA, pointB) =>
        {
            Vector3 directionA = pointA - center;
            Vector3 directionB = pointB - center;
            float angleA = Mathf.Atan2(Vector3.Dot(directionA, bitangent), Vector3.Dot(directionA, tangent));
            float angleB = Mathf.Atan2(Vector3.Dot(directionB, bitangent), Vector3.Dot(directionB, tangent));
            return angleA.CompareTo(angleB);
        });

        // Fan triangulation으로 캡 생성
        // upper 쪽: 법선이 planeNormal 방향
        AddCapToMesh(upper, uniquePoints, center, planeNormal);
        // lower 쪽: 법선이 -planeNormal 방향 (와인딩 반대)
        AddCapToMesh(lower, uniquePoints, center, -planeNormal);
    }

    private static void AddCapToMesh(MeshData meshData, List<Vector3> sortedPoints, Vector3 center, Vector3 normal)
    {
        bool flipped = Vector3.Dot(normal, Vector3.up) < 0
            ? normal.y < 0
            : normal.x + normal.z > 0;

        int centerIndex = meshData.AddVertex(center, normal, new Vector2(0.5f, 0.5f));

        int[] pointIndices = new int[sortedPoints.Count];
        for (int i = 0; i < sortedPoints.Count; i++)
        {
            pointIndices[i] = meshData.AddVertex(sortedPoints[i], normal, Vector2.zero);
        }

        for (int i = 0; i < sortedPoints.Count; i++)
        {
            int next = (i + 1) % sortedPoints.Count;
            if (flipped)
            {
                meshData.capTriangles.Add(centerIndex);
                meshData.capTriangles.Add(pointIndices[next]);
                meshData.capTriangles.Add(pointIndices[i]);
            }
            else
            {
                meshData.capTriangles.Add(centerIndex);
                meshData.capTriangles.Add(pointIndices[i]);
                meshData.capTriangles.Add(pointIndices[next]);
            }
        }
    }
}
