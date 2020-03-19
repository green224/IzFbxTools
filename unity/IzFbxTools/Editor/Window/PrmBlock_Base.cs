using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace IzFbxTools.Window {

/**
 * パラメータブロック表示モジュールの基底クラス
 */
abstract class PrmBlock_Base {

	public bool isEnable;		//!< 使用するか否か

	/** 実行できるか否か */
	virtual public bool isValidParam => true;

	/** GUI描画処理 */
	public abstract void drawGUI();



	abstract protected string name {get;}		//!< 表示名

	protected PrmBlock_Base(bool isEnable) {
		this.isEnable = isEnable;
	}

	/** 使用するか否かのトグル表示 */
	protected IDisposable showIsEnableToggle() {
		var ret = new GUILayout.VerticalScope("box");
		isEnable = EditorGUILayout.ToggleLeft( name, isEnable );
		return new MergeDispose( new IDisposable[] { ret, new IndentScope() } );
	}



	sealed class IndentScope : IDisposable {
		public IndentScope() => ++EditorGUI.indentLevel;
		public void Dispose() { if (!disposed) {--EditorGUI.indentLevel; disposed=true;} }
		bool disposed = false;
	}

	sealed class MergeDispose : IDisposable {
		public MergeDispose(IDisposable[] src) => _src = src;
		public void Dispose() {
			if (_src!=null) foreach (var i in _src.Reverse()) i.Dispose();
			_src = null;
		}
		IDisposable[] _src;
	}

}

}