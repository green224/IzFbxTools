using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core {

/**
 * 出力処理本体
 */
sealed class Root {

	// ------------------------------------- public メンバ --------------------------------------------

	/** メッシュに対して処理を行う */
	public static void procMesh(
		Mesh srcMesh,
		bool isMergeEdge, float mergeLength
	) {
		if (isMergeEdge) edgeMerge(srcMesh, mergeLength);
	}

	/** FBXに対して処理を行う */
	public static void procFBX(
		GameObject srcGObj,
		bool isCombineMesh, string dstMeshObjName,
		bool isMergeEdge, float mergeLength
	) {
		// 出力先を読み込む
//		Debug.Log(PrefabUtility.GetPrefabType( srcGObj ));throw new SystemException();
//		var srcPath = AssetDatabase.GetAssetPath( srcGObj );
//		var dstObj = PrefabUtility.LoadPrefabContents( srcPath );
//		PrefabUtility.UnpackPrefabInstance(dstObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
		var dstObj = GameObject.Instantiate( srcGObj );
		var name = dstObj.name;
		dstObj.name = name.Substring(0, name.Length - 7);

		// プレファブ全体に対してメッシュ結合を行う
		if (isCombineMesh) {
			var dstMesh = getDstMesh( getDstPath(srcGObj, ".asset") );
			var dstMeshObj = new MeshCombiner.MeshObject() {mesh = dstMesh};
			dstMeshObj.reset();
			MeshCombiner.combine(dstObj, dstMeshObj, dstMeshObjName);
		}

		// プレファブ全体に対してエッジ融合処理を行う
		if (isMergeEdge) {
			var dstMeshes = MeshComponentWrapper.getMeshComponentsInChildren(dstObj);
			foreach (var i in dstMeshes) {
				var dstMesh = edgeMerge(i.mesh, mergeLength);
				i.mesh = dstMesh;
			}
		}

		// 出力
		var dstPath = getDstPath(srcGObj, ".prefab");
		PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var success);
		GameObject.DestroyImmediate(dstObj);
	}


	// ------------------------------------- private メンバ --------------------------------------------

	/** 出力先パスを決定する */
	static string getDstPath(UnityEngine.Object srcObj, string ext) {
		var srcPath = AssetDatabase.GetAssetPath( srcObj );
		var srcName = srcObj.name;
		return
			srcPath.Substring(
				0,
				srcPath.Length - System.IO.Path.GetExtension(srcPath).Length
			) + " (Optimized)" + srcName + ext;
	}

	// Meshの出力先を読み込む
	static Mesh getDstMesh( Mesh srcMesh ) => getDstMesh( getDstPath(srcMesh, ".asset") );
	static Mesh getDstMesh( string path ) {
		var ret = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
		if (ret == null) {
			ret = new Mesh();
			AssetDatabase.CreateAsset( ret, path );
		}

		return ret;
	}

	/** 1メッシュに対してエッジ融合処理を行う */
	static Mesh edgeMerge(Mesh srcMesh, float mergeLength) {
		var dstMesh = getDstMesh(srcMesh);
		EdgeMerger.proc( srcMesh, dstMesh, mergeLength );
		return dstMesh;
	}


	// --------------------------------------------------------------------------------------------------
}

}