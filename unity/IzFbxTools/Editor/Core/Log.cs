using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Core {

/**
 * ログを生成するためのモジュール
 */
sealed class Log {

	// ------------------------------------- public メンバ --------------------------------------------

	/** Singleton実装 */
	static public Log instance {get{
		if (_instance == null) _instance = new Log();
		return _instance;
	}}

	public int lineCnt {get; private set;} = 0;		//!< ログの文字列行数

	/** 初期化してログをクリアする */
	public void reset() {
		sb_.Clear();
		lineCnt = 0;
	}

	/** 1メッシュに対してEdgeMerge処理を開始する。終わったときにendOneEdgeMergeを呼ぶこと */
	public void beginOneEdgeMerge() {
		_mergedEdgeCnt = 0;
		_mergedCornerCnt = 0;

		if ( sb_.Length != 0 ) {
			sb_.AppendLine("------------------");
			sb_.AppendLine("");
			lineCnt += 2;
		}
	}

	/** マージしたエッジ数をカウント */
	public void countMergedEdge() => ++_mergedEdgeCnt;

	/** マージしたコーナー数をカウント */
	public void countMergedCorner() => ++_mergedCornerCnt;

	/** 1メッシュに対してEdgeMerge処理を完了する */
	public void endOneEdgeMerge(bool isSuccess, Mesh srcMesh, Mesh dstMesh) {
		sb_.AppendLine( "エッジ融合" + (isSuccess ? "[成功]" : "[失敗]") + " : " + srcMesh.name + " → " + dstMesh.name );
		sb_.AppendLine("ポリゴン数: " + srcMesh.triangles.Length/3 + " → " + dstMesh.triangles.Length/3);
		sb_.AppendLine("溶接エッジ数: " + _mergedEdgeCnt);
		sb_.AppendLine("溶接コーナー数: " + _mergedCornerCnt);
		lineCnt += 4;
	}

	/** メッシュ融合処理を完了する */
	public void endCombineMesh(bool isSuccess, Mesh[] srcMeshes, Mesh dstMesh) {
		sb_.AppendLine( "メッシュ結合" + (isSuccess ? "[成功]" : "[失敗]") );
		foreach (var i in srcMeshes) sb_.AppendLine( "    " + i.name );
		sb_.AppendLine( "     → " + dstMesh.name );
		lineCnt += 2 + srcMeshes.Length;
	}

	/** 最終結果文字列を構築する */
	public string getResultStr() => sb_.ToString();


	// ------------------------------------- private メンバ --------------------------------------------

	static Log _instance = null;		//!< Singleton実装
	
	System.Text.StringBuilder sb_ = new System.Text.StringBuilder();
	int _mergedEdgeCnt, _mergedCornerCnt;


	// --------------------------------------------------------------------------------------------------
}

}