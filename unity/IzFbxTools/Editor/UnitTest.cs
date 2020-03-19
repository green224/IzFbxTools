using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Assert = UnityEngine.Assertions.Assert;

namespace IzFbxTools {

/**
 * ユニットテスト
 */
static class UnitTest {
	
	/** M.isCrossingのテスト */
	[Test] public static void M_isCrossing() {
		for (int i=0; i<1000; ++i) {
			var isCrossing = UnityEngine.Random.Range(0,2) == 0;
			float theta0, theta1;
			if ( isCrossing ) {
				theta0 = UnityEngine.Random.Range( 0.03f, 3.14f );
				theta1 = UnityEngine.Random.Range( 0.01f, theta0 - 0.01f );
			} else {
				theta0 = UnityEngine.Random.Range( -3.14f, 3.1f );
				theta1 = UnityEngine.Random.Range( Mathf.Max(theta0, 0) + 0.01f, 3.14f );
			}
			
			var ofsTheta = UnityEngine.Random.Range( -3.14f, 3.14f );
			var ofsPos = new Vector2(
				UnityEngine.Random.Range(-100, 100),
				UnityEngine.Random.Range(-100, 100)
			);

			var p0 = new Vector2(
				Mathf.Cos( ofsTheta ),
				Mathf.Sin( ofsTheta )
			);
			var p1 = -p0;
			p0 += ofsPos;
			p1 += ofsPos;

			var dir0 = new Vector2(
				Mathf.Cos( ofsTheta + theta0 ),
				Mathf.Sin( ofsTheta + theta0 )
			);
			var dir1 = new Vector2(
				Mathf.Cos( ofsTheta + theta1 ),
				Mathf.Sin( ofsTheta + theta1 )
			);

			//	if (
			//		isCrossing !=
			//		M.isCrossing(
			//			new M.HalfLine(p0, dir0),
			//			new M.HalfLine(p1, dir1)
			//		)
			//	) UnityEngine.Debug.Log( "err:"+isCrossing+"/\np0=" + p0 + " -> " +dir0 + "\np1=" + p1 + " -> " +dir1 );
			Assert.AreEqual(
				isCrossing,
				Core.M.isCrossing(
					new Core.M.HalfLine(p0, dir0),
					new Core.M.HalfLine(p1, dir1)
				)
			);
		}
	}

}

}