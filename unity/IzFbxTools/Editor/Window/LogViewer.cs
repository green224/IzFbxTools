using System;
using UnityEngine;
using UnityEditor;

namespace IzFbxTools.Window {

/**
 * ログ表示モジュール
 */
sealed class LogViewer {

	/** GUI描画処理 */
	public void drawGUI() {

		var log = Core.Log.instance;
		if ( log.lineCnt == 0 ) return;

		using (var sv = new EditorGUILayout.ScrollViewScope(_logScrollPos, "box")) {
			_logScrollPos = sv.scrollPosition;
			EditorGUILayout.SelectableLabel(
				log.getResultStr(),
				GUILayout.Height(2 + 13*(log.lineCnt+1))		// ここの正しい高さは不明。とりあえずいい感じの高さになるように適当な値を設定している。
			);
		}
	}

	Vector2 _logScrollPos = new Vector2(0, 0);
}

}