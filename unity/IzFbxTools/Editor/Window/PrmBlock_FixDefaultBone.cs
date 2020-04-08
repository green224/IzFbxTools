using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * ボーンの初期姿勢を、Meshが指定する初期姿勢に修正する
 */
[Serializable] sealed class PrmBlock_FixDefaultBone : PrmBlock_Base<Param.FixDefaultBone> {

	public PrmBlock_FixDefaultBone(bool isEnable) : base(isEnable) {}

	/** GUI描画処理 */
	override public void drawGUI() {
		using (showIsEnableToggle()){
		}
	}


	override protected string name => "ボーン初期姿勢修正";		//!< 表示名
	override protected string tooltips => "ボーンの初期姿勢を、Meshが指定する初期姿勢に修正する";		//!< 詳細説明

}

}