using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * メッシュ結合 に関するパラメータブロック表示モジュール
 */
[Serializable] sealed class PrmBlock_CombineMesh : PrmBlock_Base<Param.CombineMesh> {

	public PrmBlock_CombineMesh(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			param.dstMeshObjName = EditorGUILayout.TextField( "結合後オブジェクト名", param.dstMeshObjName );
		}
	}


	override protected string name => "単一メッシュに結合";		//!< 表示名
	override protected string tooltips => "含まれる複数のMeshを、表示状態を維持したまま1つのメッシュに結合する";		//!< 詳細説明

}

}