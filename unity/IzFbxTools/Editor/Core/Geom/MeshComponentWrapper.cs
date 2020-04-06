using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core.Geom {

/**
 * SkinnedMesh や MeshRenderer に対して、
 * 統一的にアクセスするための機能を提供するモジュール
 */
sealed class MeshComponentWrapper {

	readonly public GameObject gameObject;
	readonly public Transform transform;

	readonly public MeshRenderer meshRdr;
	readonly public MeshFilter meshFlt;
	readonly public SkinnedMeshRenderer skMeshRdr;
	readonly public Matrix4x4 l2wMtx;

	/** メッシュデータ */
	public Mesh mesh {
		get => (meshFlt==null?null:meshFlt.sharedMesh) ?? (skMeshRdr==null?null:skMeshRdr.sharedMesh);
		set {
			if (meshFlt!=null) meshFlt.sharedMesh = value;
			if (skMeshRdr!=null) skMeshRdr.sharedMesh = value;
		}
	}

	/** マテリアル一覧 */
	public Material[] materials {
		get => (meshRdr==null?null:meshRdr.sharedMaterials) ?? (skMeshRdr==null?null:skMeshRdr.sharedMaterials);
		set {
			if (meshRdr!=null) meshRdr.sharedMaterials = value;
			if (skMeshRdr!=null) skMeshRdr.sharedMaterials = value;
		}
	}

	/** ボーン情報 */
	public Transform[] bones {
		get => skMeshRdr==null ? Array.Empty<Transform>() : skMeshRdr.bones;
		set {
			if (skMeshRdr==null) {
				if (value.Length == 0) return;
				throw new SystemException();
			}
			skMeshRdr.bones = value;
		}
	}

	public MeshComponentWrapper( GameObject srcGObj, Matrix4x4 parentL2WMtx ) {
		gameObject = srcGObj;
		transform = gameObject.transform;
		l2wMtx = parentL2WMtx * getLocalMtx( transform );
			
		meshRdr = gameObject.GetComponent<MeshRenderer>();
		meshFlt = gameObject.GetComponent<MeshFilter>();
		skMeshRdr = gameObject.GetComponent<SkinnedMeshRenderer>();
	}

	/** 指定ゲームオブジェクトからMeshComponentWrapperを生成 */
	static public MeshComponentWrapper getMeshComponent(GameObject target, Matrix4x4 parentL2W) {
		var meshRdr = target.GetComponent<MeshRenderer>();
		var skMeshRdr = target.GetComponent<SkinnedMeshRenderer>();
		if (meshRdr==null && skMeshRdr==null) return null;
		return new MeshComponentWrapper(target, parentL2W);
	}

	/**
	 * 指定ゲームオブジェクト、およびその子供からMeshComponentWrapperを生成
	 * その際にヒエラルキの子供側からリストに格納する。
	 */
	static public MeshComponentWrapper[] getMeshComponentsInChildren(GameObject target) {
		var ret = new List<MeshComponentWrapper>();

		Action<Transform, Matrix4x4> oneProc = null;
		oneProc = (trans, parentL2W) => {
			var l2w = parentL2W * getLocalMtx(trans);
			for (int i=0; i<trans.childCount; ++i) oneProc(trans.GetChild(i), l2w);

			var a = getMeshComponent(trans.gameObject, parentL2W);
			if (a!=null) ret.Add(a);
		};
		oneProc(target.transform, Matrix4x4.identity);

		return ret.ToArray();
	}

	/** トランスフォームからローカルの座標変換マトリクスを得る */
	static public Matrix4x4 getLocalMtx(Transform trans) => Matrix4x4.TRS(
		trans.localPosition,
		trans.localRotation,
		trans.localScale
	);

}

}