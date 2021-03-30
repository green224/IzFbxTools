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
	public Param.FixDefaultBone fixDefaultBonePrm;

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
			new Anim.MirrorAnimGenerator(mirrorAnimPrm).proc(srcAnim, dstAnim);
			srcAnim = dstAnim;
		}
		if (visAnimPrm!=null) {
			var dstAnim = new AnimationClip();
			new Anim.VisibilityAnimGenerator(visAnimPrm).proc(srcAnim, dstAnim);
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

		// 先にFBXに含まれるアニメーションに対する処理を行う
		var mrrAnimGen = mirrorAnimPrm==null ? null : new Anim.MirrorAnimGenerator(mirrorAnimPrm);
		var visAnimGen = visAnimPrm==null ? null : new Anim.VisibilityAnimGenerator(visAnimPrm);
		{
			if (visAnimGen!=null) {
				// メッシュ結合をする場合は、Visibilityアニメ生成処理の中で、複数Vis変更カーブを結合する処理を入れておく
				if (combineMeshPrm != null) {
					int cnbCnt = 0;
					visAnimGen.getCombinedName = () => combineMeshPrm.dstMeshObjName + " (" + ++cnbCnt + ")";
				}

				{// Vis表示対象となりうるオブジェクト名をあらかじめ設定しておく
					visAnimGen.visTargetableNames = new List<string>();
					foreach (var j in dstObj.GetComponentsInChildren<SkinnedMeshRenderer>())
						visAnimGen.visTargetableNames.Add( j.gameObject.name );
					foreach (var j in dstObj.GetComponentsInChildren<MeshRenderer>())
						visAnimGen.visTargetableNames.Add( j.gameObject.name );
				}
			}

			// 元アニメーションを羅列
			var srcAnims = new List<(AnimationClip clip, bool isNeedMirror)>();
			var srcPath = AssetDatabase.GetAssetPath(srcGObj);
			foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(srcPath)) {
				var a = asset as AnimationClip;
				if (a==null || !AssetDatabase.IsSubAsset(a)) continue;
				srcAnims.Add((a,true));
			}

			// ミラーリング対象か否かを判定しておく
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
						dstAnim => visAnimGen.proc(srcAnim, dstAnim)
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

		{// FBXに含まれるメッシュに対する処理を行う
			var proc4Meshes = new List<List<Action<bool>>>();

			// プレファブ全体に対してメッシュ結合を行う
			if (combineMeshPrm!=null) {
				var procs = new List<Action<bool>>();
				procs.Add( isLast => {
					// ここの処理は複雑なため、procCmn_Meshは使わずに実装する
					Func<string, Geom.MeshCombiner.MeshObject> getDstMesh = meshName => {
						var dstMesh = getDstSubasset<Mesh>(isLast, meshName);
						var dstMeshObj = new Geom.MeshCombiner.MeshObject() {mesh = dstMesh};
						dstMeshObj.reset();
						return dstMeshObj;
					};
					if (visAnimGen == null) {
						var dstMesh = getDstMesh(combineMeshPrm.dstMeshObjName);
						Geom.MeshCombiner.combine(
							dstObj, dstMesh,
							combineMeshPrm.dstMeshObjName
						);
						if (isLast) dstMeshes.Add(dstMesh.mesh);
					} else {
						var combineRet = Geom.MeshCombiner.combine(
							dstObj, getDstMesh,
							combineMeshPrm.dstMeshObjName,
							visAnimGen.visTargetObjInfos.Select(
								i => ( i.Value.srcObjNames.ToArray(), i.Value.combinedTgtName )
							).ToArray()
						);
						if (isLast)
							foreach (var i in combineRet) dstMeshes.Add(i);
					}
				} );
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

		// ボーンの初期姿勢をメッシュの初期姿勢になるように修正する
		if (fixDefaultBonePrm!=null) {
			var bindPoseMap = new Dictionary<Transform,Matrix4x4>();
			foreach (var i in Geom.MeshComponentWrapper.getMeshComponentsInChildren(dstObj)) {
				if (i.skMeshRdr != null) {
					for (int j=0; j<i.skMeshRdr.bones.Length; ++j) {
						var b = i.skMeshRdr.bones[j];
						if (b!=null) bindPoseMap[b] = i.mesh.bindposes[j];
					}
				}
			}

			// 共通の最親ボーンを見つける
			var rootBone = bindPoseMap.First().Key;
			for (var i=rootBone; bindPoseMap.ContainsKey(i); i=i.parent) rootBone=i;

			// ルートボーンから順に、ボーン初期姿勢をフィックス
			Action<Transform, Matrix4x4> oneProc = null;
			oneProc = (bone, parentL2W) => {
				if ( bindPoseMap.TryGetValue(bone, out var bindpose) ) {
					var bindMtx = (bindpose*parentL2W).inverse;
					bone.localRotation = bindMtx.rotation;
					bone.localPosition = new Vector3(bindMtx.m03, bindMtx.m13, bindMtx.m23);
					bone.localScale = bindMtx.lossyScale;
//					parentL2W = bindpose.inverse;
					parentL2W = bone.localToWorldMatrix;

					for (int i=0; i<bone.childCount; ++i) oneProc(bone.GetChild(i), parentL2W);
				}
			};
			oneProc(rootBone, rootBone.parent?.localToWorldMatrix??Matrix4x4.identity);
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
		var srcFN = System.IO.Path.GetFileName(srcPath);
		var srcExt = System.IO.Path.GetExtension(srcPath);
		var srcName = srcPath.Substring(
			srcPath.Length - srcFN.Length,
			srcFN.Length - srcExt.Length
		);
		var srcDirName = srcPath.Substring(
			0,
			srcPath.Length - srcFN.Length
		);
		if (AssetDatabase.IsSubAsset(srcObj)) srcName = srcName + "_" + srcObj.name;
		return srcDirName + string.Format(dstAssetName, srcName) + ext;
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