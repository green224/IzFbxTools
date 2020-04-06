using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Root : EditorWindow {

    TargetTab _tgtTab = new TargetTab();	//!< 目標タイプのタブ
	PrmBlock_CombineMesh _pb_CombineMesh = new PrmBlock_CombineMesh(true);	//!< パラメータ メッシュ結合
	PrmBlock_EdgeMerge _pb_EdgeMerge = new PrmBlock_EdgeMerge(true);		//!< パラメータ 輪郭線修正
	PrmBlock_MirrorAnim _pb_MirrorAnim = new PrmBlock_MirrorAnim(true);		//!< パラメータ アニメーション左右反転
	PrmBlock_VisAnimGen _pb_VisAnimGen = new PrmBlock_VisAnimGen(true);		//!< パラメータ Visibilityアニメーション生成
	LogViewer _logViewer = new LogViewer();	//!< ログ表示モジュール

	/** 実行できるか否か */
	bool isValidParam {get{
		if (_tgtTab.target==null) return false;
		return true;
	}}

	/** メニューコマンド */
	[MenuItem("Tools/IzFbxTools")]
	static void create() {
		GetWindow<Root>("IzFbxTools");
	}

	/** GUI描画処理 */
	void OnGUI() {
		// タブを描画する
		_tgtTab.drawGUI();

		var isEnableAny = false;
		switch (_tgtTab.mode) {
			case TargetTab.Mode.Mesh:{
				_pb_EdgeMerge.drawGUI();
				isEnableAny |= _pb_EdgeMerge.isEnable;
			}break;
			case TargetTab.Mode.Anim:{
				_pb_MirrorAnim.drawGUI();
				_pb_VisAnimGen.drawGUI();
				isEnableAny |= _pb_MirrorAnim.isEnable;
				isEnableAny |= _pb_VisAnimGen.isEnable;
			}break;
			case TargetTab.Mode.Fbx:{
				_pb_CombineMesh.drawGUI();
				_pb_EdgeMerge.drawGUI();
				isEnableAny |= _pb_CombineMesh.isEnable;
				isEnableAny |= _pb_EdgeMerge.isEnable;
			}break;
			default:throw new SystemException();
		}

		using (new EditorGUI.DisabledGroupScope( !isValidParam || !isEnableAny )) {
			if (GUILayout.Button("実行")) build();
		}

		// 結果ログ
		EditorGUILayout.Space();
		_logViewer.drawGUI();
	}

	/** 出力処理本体 */
	void build() {
		Core.Log.instance.reset();

		switch (_tgtTab.mode) {
			case TargetTab.Mode.Mesh:
				Core.Root.procMesh(
					_tgtTab.tgtMesh,
					_pb_EdgeMerge.isEnable,
					_pb_EdgeMerge.mergeLength
				);
				break;
			case TargetTab.Mode.Anim:
				break;
			case TargetTab.Mode.Fbx:
				Core.Root.procFBX(
					_tgtTab.tgtGObj,
					_pb_CombineMesh.isEnable,
					_pb_CombineMesh.dstMeshObjName,
					_pb_EdgeMerge.isEnable,
					_pb_EdgeMerge.mergeLength
				);
				break;
			default:throw new SystemException();
		}

		AssetDatabase.SaveAssets();
	}

}

}