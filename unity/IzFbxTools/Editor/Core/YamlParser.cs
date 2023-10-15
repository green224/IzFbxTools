using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IzFbxTools.Core {

// UnityアセットのYamlのパーサー。
// サブアセットのIDを変えずに更新する事がどうも不可能なようなので、
// これで無理やりテキストを書き換えてIDの保持を行う
sealed class YamlParser {
	// ------------------------------------- public メンバ --------------------------------------------

	readonly public string filepath;

	public YamlParser(string filepath) {
		this.filepath = filepath;

		// 指定のアセットをYAMLとして読み込み、アニメーション・Meshの名前↔IDのマップをキャッシュ
		makeNameIdMap(
			loadFileAsText(filepath),
			out _meshNameIDMap,
			out _animNameIDMap );
	}

	// 指定のアセットをYAMLとして読み込み、
	// キャッシュ済みのアニメーション・Meshの名前↔IDのマップを復元する
	public void repairNameIDMap() {

		// 目標をテキストとして読み込み
		var text = loadFileAsText(filepath);

		// 現在の名前↔IDの対応を読み込み
		makeNameIdMap(
			loadFileAsText(filepath),
			out var curMeshNameIDMap,
			out var curAnimNameIDMap );

		// キャッシュされたときのIDへ置換
		var sb = new StringBuilder(text);
		foreach (var i in curMeshNameIDMap) {
			if (_meshNameIDMap.TryGetValue(i.Key, out var lastID))
				sb.Replace(i.Value, lastID);
		}
		foreach (var i in curAnimNameIDMap) {
			if (_animNameIDMap.TryGetValue(i.Key, out var lastID))
				sb.Replace(i.Value, lastID);
		}

		// 保存
		saveFileAsText(filepath, sb.ToString());
	}


	// ------------------------------------- private メンバ --------------------------------------------

	Dictionary<string, string> _meshNameIDMap;
	Dictionary<string, string> _animNameIDMap;


	// 指定のYAMLテキストから、アニメーション・Meshの名前↔IDのマップを作成
	static void makeNameIdMap(
		string yaml,
		out Dictionary<string, string> meshNameIDMap,
		out Dictionary<string, string> animNameIDMap
	) {
		using var rs = new StringReader(yaml);

		meshNameIDMap = new Dictionary<string, string>();
		animNameIDMap = new Dictionary<string, string>();

		string id = null;
		int assetType = -1;
		while (-1 < rs.Peek()) {
			var line = rs.ReadLine();
			if (line.StartsWith("--- !u!")) {

				// ID
				id = line.Substring( line.IndexOf('&')+1 );

				// アセットタイプ
				line = rs.ReadLine();
				if (line == "Mesh:") {	// TODO: ←これ動作未チェックなので、正しいかどうか要確認
					assetType = 0;
				} else if (line == "AnimationClip:") {
					assetType = 1;
				} else {
					assetType = -1;
				}

			} else if (assetType != -1) {

				// IDを読み込み済みなので、中身のパース
				if (line.StartsWith("  m_Name: ")) {

					// 名前
					var name = line.Substring(10);

					// マップに登録
					if (assetType == 0) {
						meshNameIDMap.Add(name, id);
					} else if (assetType == 1) {
						animNameIDMap.Add(name, id);
					} else {
						throw new InvalidProgramException();
					}

					assetType = -1;
				}
			}
		}
	}

	// 指定のファイルパスのテキストを全読み込みする
	static string loadFileAsText(string filepath) {
		using var sr = new StreamReader(filepath, Encoding.UTF8);
		return sr.ReadToEnd();
	}

	// 指定のファイルパスにテキストを保存する
	static void saveFileAsText(string filepath, string text) {
		using var sw = new StreamWriter(filepath, false, Encoding.UTF8);
		sw.Write(text);
	}


	// --------------------------------------------------------------------------------------------------
}

}
