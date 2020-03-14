using System;
using UnityEngine;

/**
 * 数学関数群
 */
static class M {

	/** 半直線 */
	public struct HalfLine {
		public Vector2 pos, dir;
		public HalfLine(Vector2 pos, Vector2 dir) {
			this.pos = pos;
			this.dir = dir;
		}
	}

	/** 内積 */
	public static float dot(Vector3 a,Vector3 b) => a.x*b.x + a.y*b.y + a.z*b.z;

	/** 外積 */
	public static Vector3 cross(Vector3 a,Vector3 b) => new Vector3(
		a.y*b.z - a.z*b.y,
		a.z*b.x - a.x*b.z,
		a.x*b.y - a.y*b.x
	);

	/** 90度回転する */
	public static Vector2 rot90(Vector2 a) => new Vector2(-a.y, a.x);
	/** 270度回転する */
	public static Vector2 rot270(Vector2 a) => new Vector2(a.y, -a.x);

	/**
	 * 二つの半直線が交差するか否か。
	 * 端点が重なる場合は交差とみなす。
	 */
	public static bool isCrossing( HalfLine l0, HalfLine l1 ) {

		var p01 = l1.pos - l0.pos;
		var p01Len = p01.magnitude;
		if ( p01Len == 0 ) return true;

		// l0の端点を原点として、l0の端点からl1の端点への方向を軸とした座標系Aを構築。
		//
		//           p1 *
		//              ↑
		//              ｜ p01n
		//              ｜
		//       p01t   ｜
		//   ←―――― * p0
		//
		var p01n = p01 / p01Len;
		var p01t = rot90(p01n);

		// p01t方向に、両直線の向きがそろっていない場合、絶対に交差しない
		var l0dt = dot(p01t,l0.dir);
		var l1dt = dot(p01t,l1.dir);
		if ( l0dt * l1dt < 0 ) return false;

		// 2直線の進行方向が狭まる方向に向かっている場合は、交差
		return 0 < dot( rot90(l0.dir), l1.dir ) * l0dt;
	}

}
