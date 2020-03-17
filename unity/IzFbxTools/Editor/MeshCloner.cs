using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools {

/**
 * Meshをコピーするための機能。
 *
 * UntyのMeshはそのままコピーする方法が無い。
 * ・AssetDataBase.SaveAssetsでそのまま保存
 *    → Meshが周囲からの参照を自身で持ってしまっているため、できない
 * ・dstMesh.Clear(); EditorUtility.CopySerialized(srcMesh, dstMesh);
 *    → できたりできなかったり。
 *       またこの方法はClearを呼ぶ必要があったり、一度目に二度呼ばなければならないなど、不安定な挙動が多い。
 *       https://answers.unity.com/questions/678967/why-meshes-require-a-restart-when-using-copyserial.html
 *       そもそもMeshはSerializedObjectじゃないのでこれで動作しないのは当然かもしれない。
 * したがって手動でコピーを行う必要がある。
 */
static class MeshCloner {

	public static void clone( Mesh srcMesh, Mesh dstMesh ) {

		dstMesh.Clear();

		dstMesh.indexFormat = srcMesh.indexFormat;
		dstMesh.vertices = srcMesh.vertices;
		dstMesh.normals = srcMesh.normals;
		dstMesh.tangents = srcMesh.tangents;
		dstMesh.uv = srcMesh.uv;
		dstMesh.uv2 = srcMesh.uv2;
		dstMesh.uv3 = srcMesh.uv3;
		dstMesh.uv4 = srcMesh.uv4;
		dstMesh.uv5 = srcMesh.uv5;
		dstMesh.uv6 = srcMesh.uv6;
		dstMesh.uv7 = srcMesh.uv7;
		dstMesh.uv8 = srcMesh.uv8;
		dstMesh.colors = srcMesh.colors;

		for (int i=0; i<srcMesh.subMeshCount; ++i)
			dstMesh.SetTriangles( srcMesh.GetTriangles(i), i );
		
		for (int i=0; i<srcMesh.blendShapeCount; ++i) {
			var fCnt = srcMesh.GetBlendShapeFrameCount(i);
			for (int j=0; j<fCnt; ++j) {
				var deltaVert = new Vector3[srcMesh.vertexCount];
				var deltaNml = new Vector3[srcMesh.vertexCount];
				var deltaTan = new Vector3[srcMesh.vertexCount];
				srcMesh.GetBlendShapeFrameVertices( i, j, deltaVert, deltaNml, deltaTan );
				dstMesh.AddBlendShapeFrame(
					srcMesh.GetBlendShapeName(i),
					srcMesh.GetBlendShapeFrameWeight(i,j),
					deltaVert,
					deltaNml,
					deltaTan
				);
			}
		}

		dstMesh.boneWeights = srcMesh.boneWeights;
		dstMesh.bindposes = srcMesh.bindposes;
		dstMesh.bounds = srcMesh.bounds;
	}

}

}