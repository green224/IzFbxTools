using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * トゥーン用輪郭線修正 に関するパラメータブロック表示モジュール
 */
[Serializable] sealed class PrmBlock_EdgeMerge : PrmBlock_Base<Param.EdgeMerge> {

	public PrmBlock_EdgeMerge(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			param.mergeLength = EditorGUILayout.FloatField( "融合距離", param.mergeLength );
		}
	}


	override protected string name => "トゥーン輪郭線断裂修正";		//!< 表示名

}

}