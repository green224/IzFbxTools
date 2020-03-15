using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ToonMeshEdgeMerger {

/**
* 複数Meshを結合するための機能。
*/
static class MeshCombiner {

	/** 結合前オブジェクトの情報 */
	public sealed class SrcData {
		public Mesh mesh;
		public Material[] matLst;
		public Matrix4x4 l2wMtx;
	}

	/** 結合後オブジェクトの情報 */
	public sealed class Result {
		public Mesh mesh;
		public Material[] matLst;
	}

	/**
	 * 結合処理。Meshを個別に指定して結合を行う
	 * 参考：https://answers.unity.com/questions/196649/combinemeshes-with-different-materials.html
	 */
	public static Result combine( SrcData[] srcMeshes, Mesh dstMesh ) {

		var matLst = new List<Material>();
		var ciLists = new List<List<CombineInstance>>();

		foreach (var i in srcMeshes) {

			for (int s = 0; s < i.mesh.subMeshCount; s++) {

				int matIdx = -1;
				for (int j = 0; j < matLst.Count; ++j) {
					if ( matLst[j].name != i.matLst[s].name ) continue;
					matIdx = j;
					break;
				}

				if (matIdx == -1) {
					matLst.Add(i.matLst[s]);
					matIdx = matLst.Count - 1;
				}
				ciLists.Add(new List<CombineInstance>());

				var ci = new CombineInstance();
				ci.transform = i.l2wMtx;
				ci.subMeshIndex = s;
				ci.mesh = i.mesh;
				ciLists[matIdx].Add(ci);
			}
		}

		// Combine by material index into per-material meshes
		// also, Create CombineInstance array for next step
		var meshes = new Mesh[matLst.Count];
		var ciLst = new CombineInstance[matLst.Count];

		for (int m=0; m<matLst.Count; ++m) {
			meshes[m] = new Mesh();
			meshes[m].CombineMeshes(ciLists[m].ToArray(), true, true);

			ciLst[m] = new CombineInstance();
			ciLst[m].mesh = meshes[m];
			ciLst[m].subMeshIndex = 0;
		}

		// Combine into one
		var ret = new Result();
		ret.mesh = dstMesh;
		ret.mesh.CombineMeshes(ciLst, false, false);

		// Destroy other meshes
		foreach (var oldMesh in meshes) {
			oldMesh.Clear();
			UnityEngine.Object.DestroyImmediate(oldMesh);
		}

		// Assign matLst
		ret.matLst = matLst.ToArray();

		// ログに追加
		Log.instance.endCombineMesh(true, srcMeshes.Select(i=>i.mesh).ToArray(), dstMesh);

		return ret;
	}

	/** 結合処理。GameObject全体に対して処理を行う */
	static public void combine( GameObject targetGO, Mesh dstMesh ) {

		// 結合元となるMesh情報を収集する。最も子供のTransformのものから順に格納する。
		var srcMeshes = MeshComponentWrapper.getMeshComponentsInChildren(targetGO);
		var isSkinned = false;
		foreach (var i in srcMeshes)
			if (i.gameObject.GetComponent<SkinnedMeshRenderer>()!=null) isSkinned=true;
		var srcDataLst = srcMeshes
			.Where( i => i.mesh.blendShapeCount==0 )	// BlendShapeがあるものは結合対象外
			.Select( i => (
				data : new SrcData(){
					mesh = i.mesh,
					matLst = i.materials,
					l2wMtx = i.l2wMtx
				},
				trans : i.transform
			) ).ToArray();

		// 結合したメッシュを生成
		var combined = combine( srcDataLst.Select(i=>i.data).ToArray(), dstMesh );

		// 結合したメッシュを表示するオブジェクトを追加
		var combinedMeshObj = new GameObject("Combined");
		combinedMeshObj.transform.SetParent( targetGO.transform, false );
		if (isSkinned) {
			var smr = combinedMeshObj.AddComponent<SkinnedMeshRenderer>();
			smr.sharedMesh = combined.mesh;
			smr.sharedMaterials = combined.matLst;
		} else {
			var mr = combinedMeshObj.AddComponent<MeshRenderer>();
			var mf = combinedMeshObj.AddComponent<MeshFilter>();
			mf.sharedMesh = combined.mesh;
			mr.sharedMaterials = combined.matLst;
		}

		// 結合前のオブジェクトを削除
		foreach (var i in srcDataLst) {
			// 子供がない場合はそのまま削除。
			// 子供の方から順番にイテレートするので、殆どのものはそのまま削除できるはず
			if (i.trans.childCount == 0) {
				UnityEngine.Object.DestroyImmediate(i.trans.gameObject);
			} else {
				var mf = i.trans.GetComponent<MeshFilter>();
				var mr = i.trans.GetComponent<MeshRenderer>();
				var smr = i.trans.GetComponent<SkinnedMeshRenderer>();
				if (mf!=null) UnityEngine.Object.DestroyImmediate(mf);
				if (mr!=null) UnityEngine.Object.DestroyImmediate(mr);
				if (smr!=null) UnityEngine.Object.DestroyImmediate(smr);
			}
		}
	}

}

}