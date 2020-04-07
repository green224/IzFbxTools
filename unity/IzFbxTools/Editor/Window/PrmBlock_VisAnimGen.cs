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
[Serializable] sealed class PrmBlock_VisAnimGen : PrmBlock_Base<Param.VisibilityAnimGenerator> {

	public PrmBlock_VisAnimGen(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()) if (isEnable) {
			param.regexPattern = EditorGUILayout.TextField(
				new GUIContent(
					"判定パターン",
					"Visibilityアニメーションに変換する元となるボーンの名前を判定するための正規表現。この正規表現でマッチしたグループ0が、対象オブジェクト名となる。SkinnedMeshRendererのEnable値へ、boneのScale.x値がそのまま転写される。"
				),
				param.regexPattern
			);
		}
	}


	override protected string name => "Visibilityアニメ変換";		//!< 表示名

}

}