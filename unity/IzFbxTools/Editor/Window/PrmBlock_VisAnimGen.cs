using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * ボーンアニメーションから、Visibilityアニメーションを生成する。
 * BlenderなどからVisibilityアニメーションをFBX経由で読み込みたい場合などに使用する。
 */
sealed class PrmBlock_VisAnimGen : PrmBlock_Base {

	public string dstAnimSuffix = " (Visibility Converted)";	//!< 変換後のアニメーションクリップ名に付けるサフィックス
	public string regexPattern = @"\[vis:([\w\-\\]+)\]";		//!< 対象ボーン名判定パターン

	public PrmBlock_VisAnimGen(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			dstAnimSuffix = EditorGUILayout.TextField(
				new GUIContent(
					"出力後サフィックス",
					"変換後のアニメーションクリップ名に付けるサフィックス"
				),
				dstAnimSuffix
			);
			regexPattern = EditorGUILayout.TextField(
				new GUIContent(
					"判定パターン",
					"Visibilityアニメーションに変換する元となるボーンの名前を判定するための正規表現。この正規表現でマッチしたグループ0が、対象オブジェクト名となる。SkinnedMeshRendererのEnable値へ、boneのScale.x値がそのまま転写される。"
				),
				regexPattern
			);
		}
	}


	override protected string name => "Visibilityアニメ変換";		//!< 表示名

}

}