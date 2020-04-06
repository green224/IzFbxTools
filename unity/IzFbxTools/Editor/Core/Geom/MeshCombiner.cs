using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core.Geom {

/**
 * 複数Meshを結合するための機能。
 * UnityのCombineMeshは、ボーンやサブメッシュを考慮しないので、
 * 自前でちゃんとした結合を行う。
 */
static class MeshCombiner {

	// ------------------------------------- public メンバ --------------------------------------------

	/** 1オブジェクトの情報 */
	public sealed class MeshObject {
		public Mesh mesh;
		public Material[] matLst;
		public Transform[] bones;
		public Matrix4x4 l2wMtx;

		/** 空の状態か否か */
		public bool isEmpty => matLst.Length==0 && mesh.subMeshCount==1 && mesh.vertexCount==0;

		/** 中身を空に初期化する */
		public void reset() {
			mesh.Clear();
			mesh.bindposes = new Matrix4x4[0];		// Bindposes配列はClearしても消えないので手動で消す
			matLst = new Material[0];
			bones = new Transform[0];
			l2wMtx = Matrix4x4.identity;
		}

		/** ちゃんとしたデータかチェックを行う */
		public bool validate() {
			// マテリアル数とサブメッシュ数はちゃんと合わせておく
			if (matLst.Length != mesh.subMeshCount) {
				if (!isEmpty) {		// ただし、reset直後はどうしてもサブメッシュ数が1になってしまうので、その場合は例外とする
					Debug.LogError(
						"マテリアル数とサブメッシュ数が異なる:name=" + mesh.name
						+ ",matCount=" + matLst.Length + ",submeshCount=" + mesh.subMeshCount
					);
					return false;
				}
			}

			// ブレンドシェイプ持ちのメッシュは扱えない
			if (mesh.blendShapeCount != 0) {
				Debug.LogError("BlendShapeが含まれている:name=" + mesh.name);
				return false;
			}

			// バインドポーズ情報はボーン数分だけちゃんとあるようにする事
			if (mesh.bindposes.Length != bones.Length) {
				Debug.LogError(
					"Bindposes情報の数がボーン数と異なる:name=" + mesh.name
					+ ",bindposeCount=" + mesh.bindposes.Length + ",boneCount=" + bones.Length
				);
				return false;
			}

			return true;
		}

		/** MeshComponentWrapperから生成する */
		static public MeshObject generate(MeshComponentWrapper src) {
			var ret = new MeshObject() {
				mesh = src.mesh,
				matLst = src.materials,
				bones = src.bones,

				// ここ、不思議なんだけどどうやらスキンメッシュの場合は
				// ヒエラルキを無視して自分自身のTransformのみを見て変形するようになっているっぽい。
				// したがって、結合時にも自分自身の変換行列のみの影響を受けるようにする
				l2wMtx =
					src.bones.Length==0 ? src.l2wMtx
					: MeshComponentWrapper.getLocalMtx(src.transform)
			};
			if (ret.mesh.subMeshCount < ret.matLst.Length) {
				// マテリアルが必要より多く設定されている。
				// この場合はマテリアルの一致判定は可能なため、正常な範囲にトリムするだけでOK。
				// 逆にマテリアルが足りない場合はマテリアルの一致判定ができないため、
				// 何もせずに流して後でエラーにする。
				Array.Resize( ref ret.matLst, ret.mesh.subMeshCount );
			}
			return ret;
		}
	}

	/** MeshObject from を to へ結合する */
	public static bool addTo(MeshObject from, MeshObject to ) {
//		to.mesh.indexFormat = UnityEngine.Rendering.lndexFormat.UInt32;
		if (!from.validate() || !to.validate()) return false;
		if (from.isEmpty) return true;			// 無いと思うが、一応空を結合しようとした場合は何もしない

		// 頂点数を変えると帰ってくる配列が変わってしまうので、加工前に元データを全部読み込む
		var vertices = to.mesh.vertices;
		var normals = to.mesh.normals;
		var tangents = to.mesh.tangents;
		var uv1 = to.mesh.uv;  var uv2 = to.mesh.uv2; var uv3 = to.mesh.uv3; var uv4 = to.mesh.uv4;
		var uv5 = to.mesh.uv5; var uv6 = to.mesh.uv6; var uv7 = to.mesh.uv7; var uv8 = to.mesh.uv8;
		var colors = to.mesh.colors;
		var boneWgt = to.mesh.boneWeights.ToList();

		// 位置・法線・接線の結合。双方のL2W行列を考慮して結合する
		var oldToVertCnt = to.mesh.vertexCount;
		var frm2toMtx = to.l2wMtx.inverse * from.l2wMtx;
		to.mesh.vertices = vertices.Concat( from.mesh.vertices.Select(i=>frm2toMtx.MultiplyPoint(i)) ).ToArray();
		to.mesh.normals = normals.Concat( from.mesh.normals.Select(i=>frm2toMtx.MultiplyVector(i)) ).ToArray();
		to.mesh.tangents = tangents.Concat( from.mesh.tangents.Select(i=>{
			var a = frm2toMtx.MultiplyVector(i);
			return new Vector4(a.x, a.y, a.z, i.w);
		}) ).ToArray();

		// UV・カラーの結合
		to.mesh.uv = uv1.Concat( from.mesh.uv ).ToArray();
		to.mesh.uv2 = uv2.Concat( from.mesh.uv2 ).ToArray();
		to.mesh.uv3 = uv3.Concat( from.mesh.uv3 ).ToArray();
		to.mesh.uv4 = uv4.Concat( from.mesh.uv4 ).ToArray();
		to.mesh.uv5 = uv5.Concat( from.mesh.uv5 ).ToArray();
		to.mesh.uv6 = uv6.Concat( from.mesh.uv6 ).ToArray();
		to.mesh.uv7 = uv7.Concat( from.mesh.uv7 ).ToArray();
		to.mesh.uv8 = uv8.Concat( from.mesh.uv8 ).ToArray();
		to.mesh.colors = colors.Concat( from.mesh.colors ).ToArray();

		// ボーンの結合
		var (bones, fromBoneIdxes) = combineDistinctArray( to.bones, from.bones );
		to.bones = bones;

		// ボーンウェイトの結合
		foreach (var i in from.mesh.boneWeights) {
			var a = i;
			a.boneIndex0 = fromBoneIdxes[i.boneIndex0];
			a.boneIndex1 = fromBoneIdxes[i.boneIndex1];
			a.boneIndex2 = fromBoneIdxes[i.boneIndex2];
			a.boneIndex3 = fromBoneIdxes[i.boneIndex3];
			boneWgt.Add(a);
		}
		to.mesh.boneWeights = boneWgt.ToArray();

		// バインドポーズの結合
		// ここ、何も考えずにバインドポーズを上書きしているが、問題が生じたらなんらかの対処を行う
		var bindposes = new Matrix4x4[bones.Length];
		for (int i=0; i<to.mesh.bindposes.Length; ++i) bindposes[i] = to.mesh.bindposes[i];
		for (int i=0; i<fromBoneIdxes.Length; ++i) bindposes[fromBoneIdxes[i]] = from.mesh.bindposes[i]*frm2toMtx.inverse;
		to.mesh.bindposes = bindposes.ToArray();

		// マテリアルの結合
		var (matLst, fromMatIdxes) = combineDistinctArray( to.matLst, from.matLst );
		to.matLst = matLst;

		// ポリゴンの結合
		var oldToSubMeshCnt = to.isEmpty ? 0 : to.mesh.subMeshCount;
		to.mesh.subMeshCount = matLst.Length;
		for (int i=0; i<from.mesh.subMeshCount; ++i) {
			var sbIdx = fromMatIdxes[i];
			var tri = from.mesh.GetTriangles(i).Select(a=>a+oldToVertCnt).ToList();
			if (sbIdx < oldToSubMeshCnt)
				to.mesh.SetTriangles(to.mesh.GetTriangles(sbIdx).Concat(tri).ToArray(), sbIdx);
			else
				to.mesh.SetTriangles(tri, sbIdx);
		}

		return true;
	}

	/** 結合処理。GameObject全体に対して処理を行う。dstは初期化される */
	static public bool combine(
		GameObject targetGO,
		MeshObject dst,
		string dstMeshObjName	//!< 結合後に生成されるプレファブ内の、メッシュ表示用のGameObjectの名前
	) {

		// 結合元となるMesh情報を収集する。最も子供のTransformのものから順に格納する。
		var srcMeshes = MeshComponentWrapper.getMeshComponentsInChildren(targetGO);
		var srcDataLst = srcMeshes
			.Where( i => i.mesh.blendShapeCount==0 )	// BlendShapeがあるものは結合対象外
			.Select( i => (data:MeshObject.generate(i), trans:i.transform) ).ToArray();

		// 結合したメッシュを生成
		dst.reset();
		var isSuccess = true;
		foreach (var i in srcDataLst) isSuccess &= addTo(i.data, dst);

		// ログに追加
		Log.instance.endCombineMesh(isSuccess, srcDataLst.Select(i=>i.data.mesh).ToArray(), dst.mesh);
		if (!isSuccess) return false;

		// 結合したメッシュを表示するオブジェクトを追加
		var combinedMeshObj = new GameObject(dstMeshObjName);
		combinedMeshObj.transform.SetParent( targetGO.transform, false );
		if (dst.bones.Length == 0) {
			var mr = combinedMeshObj.AddComponent<MeshRenderer>();
			var mf = combinedMeshObj.AddComponent<MeshFilter>();
			mf.sharedMesh = dst.mesh;
			mr.sharedMaterials = dst.matLst;
		} else {
			var smr = combinedMeshObj.AddComponent<SkinnedMeshRenderer>();
			smr.sharedMesh = dst.mesh;
			smr.sharedMaterials = dst.matLst;
			smr.bones = dst.bones;
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

		return true;
	}


	// ------------------------------------- private メンバ --------------------------------------------

	/**
	 * それぞれに重複のない配列を二つ受け取り、重複なく1つに結合し、
	 * 結合後の配列と、片方のインデックス変換マップを返す
	 */
	static (T[] ary, int[] bIdxMap) combineDistinctArray<T>(T[] a, T[] b) where T : class {
		var retAry = a.ToList();
		var bIdxMap = new int[ b.Length ];		// bを結合後に何番にふり直すかのマップ
		for (int i=0; i<b.Length; ++i) {
			bool alreadyContains = false;
			for (int j=0; j<retAry.Count; ++j) {
				if (retAry[j] != b[i]) continue;
				bIdxMap[i] = j;
				alreadyContains = true;
				break;
			}
			if (!alreadyContains) {
				bIdxMap[i] = retAry.Count;
				retAry.Add(b[i]);
			}
		}
		return (retAry.ToArray(), bIdxMap);
	}


	// --------------------------------------------------------------------------------------------------
}

}