using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace ToonMeshEdgeMerger {

/**
 * トゥーン用の輪郭線が、ソリッド辺で途切れてしまう問題の対処を行うためのモジュール。
 * 途切れてしまう輪郭線部分に輪郭線用のポリゴンを仕込むことで、辺が途切れないようにする。
 */
sealed class Window : EditorWindow {

	Mesh _srcMesh = null;					//!< 変換対象のMesh
	float _mergeLength = 0.00001f;			//!< マージ距離
	LogBuilder _log = new LogBuilder();		//!< 結果ログ

	/** メニューコマンド */
	[MenuItem("Tools/トゥーン輪郭線用 Solid辺融合")]
	static void create() {
		GetWindow<Window>("トゥーン輪郭線用 Solid辺融合");
	}

	/** GUI描画処理 */
	void OnGUI() {
		_srcMesh = EditorGUILayout.ObjectField( "元Mesh", _srcMesh, typeof( Mesh ), false ) as Mesh;
		_mergeLength = EditorGUILayout.FloatField( "融合距離", _mergeLength );

		using (new EditorGUI.DisabledGroupScope(_srcMesh == null)) {
			if (GUILayout.Button("実行")) {
				build();
			}
		}

		// 結果ログ
		EditorGUILayout.Space();
		if ( _log.lineCnt != 0 ) {
			using (new GUILayout.VerticalScope("box")) {
				EditorGUILayout.SelectableLabel(
					_log.getResultStr(),
					GUILayout.Height(EditorGUIUtility.singleLineHeight * _log.lineCnt)
				);
			}
		}
	}

	/** 出力処理本体 */
	void build() {
		_log.reset();

		// 出力先パスを決定
		var dstMeshPath = AssetDatabase.GetAssetPath( _srcMesh );
		dstMeshPath =
			dstMeshPath.Substring(
				0,
				dstMeshPath.Length - System.IO.Path.GetExtension(dstMeshPath).Length
			) + " (EdgeMerged)" + _srcMesh.name + ".asset";

		// 出力先を読み込む
		var dstMesh = AssetDatabase.LoadAssetAtPath(dstMeshPath, typeof(Mesh)) as Mesh;
		if (dstMesh == null) {
			dstMesh = new Mesh();
			AssetDatabase.CreateAsset( dstMesh, dstMeshPath );
		}

		// 1メッシュに対して処理を実施
		EdgeMerger.proc( _srcMesh, dstMesh, _log, _mergeLength );
		AssetDatabase.SaveAssets();
	}

}

}