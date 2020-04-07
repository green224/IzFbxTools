using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * トゥーン用輪郭線修正 に関するパラメータブロック表示モジュール
 */
[Serializable] sealed class PrmBlock_MirrorAnim : PrmBlock_Base<Param.MirrorAnimGenerator> {

	public PrmBlock_MirrorAnim(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			param.shiftCycleOffset = EditorGUILayout.Toggle(
				new GUIContent(
					"再生位置シフト",
					"ミラーリング時に再生位置を半分シフトするか否か。必ず右足から移動したい等の理由で使用する"
				),
				param.shiftCycleOffset
			);
			param.suffixL = EditorGUILayout.TextField(
				new GUIContent(
					"サフィックス 左",
					"ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス"
				),
				param.suffixL
			);
			param.suffixR = EditorGUILayout.TextField(
				new GUIContent(
					"サフィックス 右",
					"ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス"
				),
				param.suffixR
			);
		}
	}


	override protected string name => "左右反転アニメ生成";		//!< 表示名

}

}