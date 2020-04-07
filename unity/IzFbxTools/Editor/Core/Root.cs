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
		var dstPath = getDstPath(srcAnim,".asset");
		
		if (mirrorAnimPrm!=null) {
			var dstAnim = new AnimationClip();
			new Anim.MirrorAnimGenerator(mirrorAnimPrm).proc( srcAnim, dstAnim );
			srcAnim = dstAnim;
		}
		if (visAnimPrm!=null) {
			var dstAnim = new AnimationClip();
			Anim.VisibilityAnimGenerator.proc( srcAnim, dstAnim, visAnimPrm.regexPattern );
			srcAnim = dstAnim;
		}

		{// 最後に出力先にコピーする
			var dstAnim = getDstAsset<AnimationClip>(dstPath);
			Anim.AnimCloner.clone( srcAnim, dstAnim );
		}
	}

	/** FBXに対して処理を行う */
	public void procFBX( GameObject srcGObj ) {
		// 出力先を読み込む
		var dstObj = GameObject.Instantiate( srcGObj );
		var name = dstObj.name;
		dstObj.name = name.Substring(0, name.Length - 7);


		// 出力先を取得する処理
		var dstPath = getDstPath(srcGObj, ".prefab");
		_oldSubassets = AssetDatabase.LoadAllAssetsAtPath(dstPath);

		// 共通処理部分
		var dstMeshes = new List<Mesh>();
		var dstAnims = new List<AnimationClip>();
		Func<bool,string,Action<Mesh>,Mesh> procCmn_Mesh = (isLast,dstName,procBody) => {
			var dstMesh = getDstSubasset<Mesh>(isLast, dstName);
			procBody(dstMesh);
			if (isLast) dstMeshes.Add(dstMesh);
			return dstMesh;
		};
		Func<bool,string,Action<AnimationClip>,AnimationClip> procCmn_Anim = (isLast,dstName,procBody) => {
			var dstAnim = getDstSubasset<AnimationClip>(isLast, dstName);
			procBody(dstAnim);
			dstAnims.Add(dstAnim);
			return dstAnim;
		};

		{// FBXに含まれるメッシュに対する処理を行う
			var proc4Meshes = new List<List<Action<bool>>>();

			// プレファブ全体に対してメッシュ結合を行う
			if (combineMeshPrm!=null) {
				var procs = new List<Action<bool>>();
				procs.Add( isLast => procCmn_Mesh(
					isLast, combineMeshPrm.dstMeshObjName, dstMesh => {
						var dstMeshObj = new Geom.MeshCombiner.MeshObject() {mesh = dstMesh};
						dstMeshObj.reset();
						Geom.MeshCombiner.combine(dstObj, dstMeshObj, combineMeshPrm.dstMeshObjName);
					}
				) );
				proc4Meshes.Add(procs);
			}

			// 個別処理部分
			if (edgeMergePrm!=null) {
				var procs = new List<Action<bool>>();
				procs.Add( isLast => {
					foreach (var i in Geom.MeshComponentWrapper.getMeshComponentsInChildren(dstObj)) {
						procCmn_Mesh(
							isLast, i.mesh.name,
							dstMesh => {
								Geom.EdgeMerger.proc(i.mesh, dstMesh, edgeMergePrm.mergeLength);
								i.mesh = dstMesh;
							}
						);
					}
				} );
				proc4Meshes.Add(procs);
			}

			for (int i=0; i<proc4Meshes.Count; ++i) {
				foreach (var j in proc4Meshes[i]) j( i == proc4Meshes.Count-1 );
			}
		}

		{// FBXに含まれるアニメーションに対する処理を行う
			// 元アニメーションを羅列
			var srcAnims = new List<(AnimationClip clip, bool isNeedMirror)>();
			var srcPath = AssetDatabase.GetAssetPath(srcGObj);
			foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(srcPath)) {
				var a = asset as AnimationClip;
				if (a==null || !AssetDatabase.IsSubAsset(a)) continue;
				srcAnims.Add((a,true));
			}

			// ミラーリング対象か否かを判定しておく
			var mrrAnimGen = mirrorAnimPrm==null
				? null : new Anim.MirrorAnimGenerator(mirrorAnimPrm);
			if (mrrAnimGen!=null) for (int i=0; i<srcAnims.Count; ++i) {
				var a = srcAnims[i];
				if (!a.isNeedMirror) continue;

				var isL = mrrAnimGen.isNameL( a.clip.name );
				var isR = mrrAnimGen.isNameR( a.clip.name );
				if (!isL && !isR) {
					srcAnims[i] = (a.clip, false);
					continue;
				}

				var mrrName = mrrAnimGen.mirrorName( a.clip.name );
				for (int j=i+1; j<srcAnims.Count; ++j) {
					var b = srcAnims[j];
					if (!b.isNeedMirror) continue;
					if (b.clip.name == mrrName) {
						srcAnims[i] = (a.clip, false);
						b = (srcAnims[j].clip, false);
						break;
					}
				}
			}

			// 全アニメーションに対して処理を行う
			foreach (var i in srcAnims) {
				var srcAnim = i.clip;
				if (visAnimPrm!=null) {
					srcAnim = procCmn_Anim(
						true, srcAnim.name,
						dstAnim => Anim.VisibilityAnimGenerator.proc(srcAnim, dstAnim, visAnimPrm.regexPattern)
					);
				}
				if (i.isNeedMirror && mrrAnimGen!=null) {
					srcAnim = procCmn_Anim(
						true, mrrAnimGen.mirrorName(srcAnim.name),
						dstAnim => mrrAnimGen.proc( srcAnim, dstAnim )
					);
				}
			}
		}

		// 出力先にある未保存対象のサブアセットを全削除
		foreach (var i in AssetDatabase.LoadAllAssetsAtPath(dstPath)) {
			if (!AssetDatabase.IsSubAsset(i)) continue;
			if (dstMeshes.Contains(i) || dstAnims.Contains(i)) continue;
			GameObject.DestroyImmediate(i, true);
		}

		// アセットが何もない状態だとサブアセットを保存できないので、何もな時には先に保存しておく。
		// ただし保存する際にはサブアセットを保存した後じゃないと正常に保存できない・・・
		// なので最後にももう一度保存する。
		if (AssetDatabase.GetMainAssetTypeAtPath(dstPath) == null) {
			PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var __success);
		}

		// アセットを保存
		foreach (var i in dstMeshes) {
			if (_oldSubassets.Contains(i)) continue;		// 既に保存済みの場合は何もしない
			AssetDatabase.AddObjectToAsset(i, dstPath);
		}
		foreach (var i in dstAnims) {
			if (_oldSubassets.Contains(i)) continue;		// 既に保存済みの場合は何もしない
			AssetDatabase.AddObjectToAsset(i, dstPath);
		}
		PrefabUtility.SaveAsPrefabAsset(dstObj, dstPath, out var success);
		GameObject.DestroyImmediate(dstObj);
	}


	// ------------------------------------- private メンバ --------------------------------------------

	/** 出力先に既に配置されているサブアセット類。途中計算用キャッシュ */
	UnityEngine.Object[] _oldSubassets;

	/** 出力先のアセットを取得する */
	T getDstSubasset<T>( bool isLast, string name ) where T : UnityEngine.Object, new() {
		if (isLast) foreach (var i in _oldSubassets)
			if (i.GetType()==typeof(T) && i.name==name) return (T)i;
		var ret = new T();
		ret.name = name;
		return ret;
	}

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