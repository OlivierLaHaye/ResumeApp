// Copyright (C) Olivier La Haye
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ResumeApp.Helpers
{
	public static class PathExtremesCalculatorHelper
	{
		internal static Point HighestPoint( Path pPath ) => GetExtremePoint( pPath, pPoint => pPoint.Y, false );

		internal static Point LowestPoint( Path pPath ) => GetExtremePoint( pPath, pPoint => pPoint.Y, true );

		internal static Point RightmostPoint( Path pPath ) => GetExtremePoint( pPath, pPoint => pPoint.X, true );

		internal static Point LeftmostPoint( Path pPath ) => GetExtremePoint( pPath, pPoint => pPoint.X, false );

		private static Point GetExtremePoint( Path pPath, Func<Point, double> pSelector, bool pDescending )
		{
			var lExtremePoint = new Point();
			bool lHasValue = false;

			foreach ( var lPoint in GetPointsFromPath( pPath ) )
			{
				if ( lHasValue && ( pDescending
						? !( pSelector( lPoint ) > pSelector( lExtremePoint ) )
						: !( pSelector( lPoint ) < pSelector( lExtremePoint ) ) ) )
				{
					continue;
				}

				lExtremePoint = lPoint;
				lHasValue = true;
			}

			return lExtremePoint;
		}

		private static IEnumerable<Point> GetPointsFromPath( Path pPath )
		{
			if ( pPath?.Data == null )
			{
				yield break;
			}

			var lGeometry = pPath.Data as PathGeometry ?? PathGeometry.CreateFromGeometry( pPath.Data );
			if ( lGeometry == null || lGeometry.Figures.Count == 0 )
			{
				yield break;
			}

			foreach ( var lFigure in lGeometry.Figures )
			{
				yield return lFigure.StartPoint;

				foreach ( var lSegment in lFigure.Segments )
				{
					switch ( lSegment )
					{
						case LineSegment lLine:
							{
								yield return lLine.Point;
								break;
							}
						case PolyLineSegment lPolyLine:
							{
								foreach ( var lPointInPolyLine in lPolyLine.Points )
								{
									yield return lPointInPolyLine;
								}
								break;
							}
						case BezierSegment lBezier:
							{
								yield return lBezier.Point1;
								yield return lBezier.Point2;
								yield return lBezier.Point3;
								break;
							}
						case PolyBezierSegment lPolyBezier:
							{
								foreach ( var lPointInPolyBezier in lPolyBezier.Points )
								{
									yield return lPointInPolyBezier;
								}
								break;
							}
						case QuadraticBezierSegment lQuadratic:
							{
								yield return lQuadratic.Point1;
								yield return lQuadratic.Point2;
								break;
							}
						case PolyQuadraticBezierSegment lPolyQuadratic:
							{
								foreach ( var lPointInPolyQuadratic in lPolyQuadratic.Points )
								{
									yield return lPointInPolyQuadratic;
								}
								break;
							}
						case ArcSegment lArc:
							{
								yield return lArc.Point;
								break;
							}
					}
				}
			}
		}
	}
}
