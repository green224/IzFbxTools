using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * トゥーン用輪郭線修正 に関するパラメータブロック表示モジュール
 */
sealed class PrmBlock_MirrorAnim : PrmBlock_Base {

	public bool shiftCycleOffset = true;	//!< ミラーリング時に開始時間を半分オフセットするか否か

	// ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス
	public string suffixL = "_L";
	public string suffixR = "_R";

	public PrmBlock_MirrorAnim(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			shiftCycleOffset = EditorGUILayout.Toggle(
				new GUIContent(
					"再生位置シフト",
					"ミラーリング時に再生位置を半分シフトするか否か。必ず右足から移動したい等の理由で使用する"
				),
				shiftCycleOffset
			);
			suffixL = EditorGUILayout.TextField(
				new GUIContent(
					"サフィックス 左",
					"ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス"
				),
				suffixL
			);
			suffixR = EditorGUILayout.TextField(
				new GUIContent(
					"サフィックス 右",
					"ミラーリング対象となる左右ボーン・アニメの名前に付くサフィックス"
				),
				suffixR
			);
		}
	}


	override protected string name => "左右反転アニメ生成";		//!< 表示名

}

}