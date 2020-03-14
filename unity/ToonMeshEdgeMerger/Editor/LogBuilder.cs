using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ToonMeshEdgeMerger {

/**
 * ログを生成するためのモジュール
 */
sealed class LogBuilder {

	// ------------------------------------- public メンバ --------------------------------------------

	public int lineCnt {get; private set;} = 0;		//!< ログの文字列行数

	/** 初期化してログをクリアする */
	public void reset() {
		sb_.Clear();
		lineCnt = 0;
	}

	/** 1メッシュに対して処理を開始する。終わったときにendOneProcを呼ぶこと */
	public void beginOneProc(Mesh src, Mesh dst) {
		_srcMesh = src;
		_dstMesh = dst;
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

	/** 1メッシュに対して処理を完了する */
	public void endOneProc(bool isSuccess) {
		sb_.AppendLine( (isSuccess ? "[成功]" : "[失敗]") + " : " + _srcMesh.name + " → " + _dstMesh.name );
		sb_.AppendLine("ポリゴン数: " + _srcMesh.triangles.Length/3 + " → " + _dstMesh.triangles.Length/3);
		sb_.AppendLine("溶接エッジ数: " + _mergedEdgeCnt);
		sb_.AppendLine("溶接コーナー数: " + _mergedCornerCnt);
		lineCnt += 4;
	}

	/** 最終結果文字列を構築する */
	public string getResultStr() => sb_.ToString();


	// ------------------------------------- private メンバ --------------------------------------------

	System.Text.StringBuilder sb_ = new System.Text.StringBuilder();
	Mesh _srcMesh, _dstMesh;
	int _mergedEdgeCnt, _mergedCornerCnt;


	// --------------------------------------------------------------------------------------------------
}

}