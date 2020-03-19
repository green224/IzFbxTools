using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * メッシュ結合 に関するパラメータブロック表示モジュール
 */
sealed class PrmBlock_CombineMesh : PrmBlock_Base {

	public string dstMeshObjName = "Combined";		//!< 結合後のメッシュ名

	public PrmBlock_CombineMesh(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			dstMeshObjName = EditorGUILayout.TextField( "結合後オブジェクト名", dstMeshObjName );
		}
	}


	override protected string name => "単一メッシュに結合";		//!< 表示名

}

}