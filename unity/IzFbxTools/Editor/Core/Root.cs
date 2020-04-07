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

	public string dstAssetName;		//!< 出力先アセット名のフォーマット文字列

	// 変換パラメータ群
	public Param.EdgeMerge edgeMergePrm;
	public Param.CombineMesh combineMeshPrm;
	public Param.MirrorAnimGenerator mirrorAnimPrm;
	public Param.VisibilityAnimGenerator visAnimPrm;

	/** メッシュに対して処理を行う */
	public void procMesh( Mesh srcMesh ) {
		if (edgeMergePrm!=null) {
			var dstMesh = getDstAsset<Mesh>(getDstPath(srcMesh,".asset"));
			Geom.EdgeMerger.proc( srcMesh, dstMesh, edgeMergePrm.mergeLength );
		}
	}

	/** アニメーションに対して処理を行う */
	public void procAnim( AnimationClip srcAnim ) {
		if (mirrorAnimPrm!=null) {
			var dstAnim = getDstAsset<AnimationClip>(getDstPath(srcAnim,".asset"));
			new Anim.MirrorAnimGenerator(
				mirrorAnimPrm.suffixL,
				mirrorAnimPrm.suffixR,
				mirrorAnimPrm.shiftCycleOffset
			).proc( srcAnim, dstAnim );
		}
		if (visAnimPrm!=null) {
			var dstAnim = getDstAsset<AnimationClip>(getDstPath(srcAnim,".asset"));
			Anim.VisibilityAnimGenerator.proc( srcAnim, dstAnim, visAnimPrm.regexPattern );
		}
	}

	/** FBXに対して処理を行う */
	public void procFBX( GameObject srcGObj ) {
		// 出力先を読み込む
		var dstObj = GameObject.Instantiate( srcGObj );
		var name = dstObj.name;
		dstObj.name = name.Substring(0, name.Length - 7);

		// プレファブ全体に対してメッシュ結合を行う
		var dstMeshes = new List<Mesh>();
		if (combineMeshPrm!=null) {
			var dstMesh = new Mesh();
			dstMesh.name = combineMeshPrm.dstMeshObjName;
			var dstMeshObj = new Geom.MeshCombiner.MeshObject() {mesh = dstMesh};
			dstMeshObj.reset();
			Geom.MeshCombiner.combine(dstObj, dstMeshObj, combineMeshPrm.dstMeshObjName);
			dstMeshes.Add(dstMesh);
		}

		// プレファブ全体に対してエッジ融合処理を行う
		if (edgeMergePrm!=null) {
			foreach (var i in Geom.MeshComponentWrapper.getMeshComponentsInChildren(dstObj)) {
				// メッシュ結合した物をさらにエッジ融合する場合は、
				// 最終処理後のMeshのみ保存するようにする
				if (dstMeshes.Contains( i.mesh )) dstMeshes.Remove(i.mesh);

				var dstMesh = new Mesh();
				dstMesh.name = i.mesh.name;
				Geom.EdgeMerger.proc( i.mesh, dstMesh, edgeMergePrm.mergeLength );
				dstMeshes.Add( i.mesh = dstMesh );
			}
		}

		// 出力先にあるサブアセットを全削除
		var dstPath = getDstPath(srcGObj, ".prefab");
		foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(dstPath)) {
			if (AssetDatabase.IsSubAsset(asset))
				GameObject.DestroyImmediate(asset, true);
		}

		// 出力
		if (AssetDatabase.GetMainAssetTypeAtPath(dstPath) == null) {
			// アセットが何もない状態だとサブアセットを保存できないので、何もな時には先に保存しておく。
			// ただし保存する際にはサブアセットを保存した後じゃないと正常に保存できない・・・
			// なので最後にももう一度保存する。
			PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var __success);
		}
		foreach (var i in dstMeshes) AssetDatabase.AddObjectToAsset(i, dstPath);
		PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var success);
		GameObject.DestroyImmediate(dstObj);
	}


	// ------------------------------------- private メンバ --------------------------------------------

	/** 出力先パスを決定する */
	string getDstPath(UnityEngine.Object srcObj, string ext) {
		var srcPath = AssetDatabase.GetAssetPath( srcObj );
		var srcName = srcPath.Substring(
			0,
			srcPath.Length - System.IO.Path.GetExtension(srcPath).Length
		);
		if (AssetDatabase.IsSubAsset(srcObj)) srcName = srcName + "_" + srcObj.name;
		return string.Format(dstAssetName, srcName) + ext;
	}

	/** Assetの出力先を読み込む */
	T getDstAsset<T>( string path ) where T : UnityEngine.Object, new() {
		var ret = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;
		if (ret == null) {
			ret = new T();
			AssetDatabase.CreateAsset( ret, path );
		}

		return ret;
	}


	// --------------------------------------------------------------------------------------------------
}

}