// Copyright (C) Olivier La Haye
// All rights reserved.

using ResumeApp.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ResumeApp.Converters
{
	[ValueConversion( typeof( object[] ), typeof( bool ) )]
	public class ValuesEqualConverter : IMultiValueConverter
	{
		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues.Length == 2 && pValues[ 0 ] != null && pValues[ 1 ] != null )
			{
				return pValues[ 0 ].Equals( pValues[ 1 ] );
			}

			return false;
		}

		public object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	[Flags]
	public enum CornerSelection
	{
		None = 0,
		TopLeft = 1,
		TopRight = 2,
		BottomRight = 4,
		BottomLeft = 8,
		All = TopLeft | TopRight | BottomRight | BottomLeft
	}

	[ValueConversion( typeof( object ), typeof( Geometry ) )]
	public sealed class SquircleCornerDataConverter : IValueConverter
	{
		private const double DefaultControlOffset = 17.25;
		private const double CanvasSize = 100.0;

		private static CornerSelection ParseCorners( object pInput )
		{
			switch ( pInput )
			{
				case null:
					{
						return CornerSelection.All;
					}
				case CornerSelection lDirect:
					{
						return lDirect == CornerSelection.None ? CornerSelection.All : lDirect;
					}
				case int lMask:
					{
						CornerSelection lFromInt = ( CornerSelection )lMask;
						return lFromInt == CornerSelection.None ? CornerSelection.All : lFromInt;
					}
				case string lTextRaw:
					{
						string lText = lTextRaw.Trim();
						if ( lText.Length == 0 )
						{
							return CornerSelection.All;
						}

						bool lHasParsedInt = int.TryParse( lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var lParsedInt );
						if ( lHasParsedInt )
						{
							CornerSelection lFromInt = ( CornerSelection )lParsedInt;
							return lFromInt == CornerSelection.None ? CornerSelection.All : lFromInt;
						}

						string[] lTokens = lText.ToLowerInvariant().Split( new[] { ',', ';', '|', ' ', '+' }, StringSplitOptions.RemoveEmptyEntries );

						bool lIsTopLeft = HasCornerTokenEnabled( lTokens, "topleft", "tl" );
						bool lIsTopRight = HasCornerTokenEnabled( lTokens, "topright", "tr" );
						bool lIsBottomRight = HasCornerTokenEnabled( lTokens, "bottomright", "br" );
						bool lIsBottomLeft = HasCornerTokenEnabled( lTokens, "bottomleft", "bl" );

						CornerSelection lResult = CornerSelection.None;
						if ( lIsTopLeft )
						{
							lResult |= CornerSelection.TopLeft;
						}
						if ( lIsTopRight )
						{
							lResult |= CornerSelection.TopRight;
						}
						if ( lIsBottomRight )
						{
							lResult |= CornerSelection.BottomRight;
						}
						if ( lIsBottomLeft )
						{
							lResult |= CornerSelection.BottomLeft;
						}

						return lResult == CornerSelection.None ? CornerSelection.All : lResult;
					}
				default:
					{
						return CornerSelection.All;
					}
			}
		}

		private static bool HasCornerTokenEnabled( string[] pTokens, string pFullName, string pShortName )
		{
			bool lHasNameOnly = pTokens.Any( pEntry => pEntry == pFullName || pEntry == pShortName );
			if ( lHasNameOnly )
			{
				return true;
			}

			foreach ( string lPair in pTokens.Where( pEntry => pEntry.Contains( "=" ) ) )
			{
				string[] lSplit = lPair.Split( new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries );
				if ( lSplit.Length != 2 )
				{
					continue;
				}

				string lKey = lSplit[ 0 ];
				string lValue = lSplit[ 1 ];
				if ( lKey != pFullName && lKey != pShortName )
				{
					continue;
				}

				if ( lValue == "1" )
				{
					return true;
				}

				bool lHasParsed = bool.TryParse( lValue, out var lParsedBool );
				if ( lHasParsed && lParsedBool )
				{
					return true;
				}
			}

			return false;
		}

		private static double ParseControlOffset( object pParameter )
		{
			switch ( pParameter )
			{
				case null:
					{
						return ClampControlOffset( DefaultControlOffset );
					}
				case double lDouble:
					{
						return ClampControlOffset( lDouble );
					}
				case string lText:
					{
						bool lHasParsed = double.TryParse( lText, NumberStyles.Float, CultureInfo.InvariantCulture, out var lParsedValue );
						if ( lHasParsed )
						{
							return ClampControlOffset( lParsedValue );
						}
						break;
					}
			}

			return ClampControlOffset( DefaultControlOffset );
		}

		private static double ClampControlOffset( double pOffset )
		{
			const double lMax = CanvasSize * 0.5;
			if ( pOffset < 0.0 )
			{
				return 0.0;
			}
			return pOffset > lMax ? lMax : pOffset;
		}

		private static Geometry BuildGeometry( CornerSelection pCorners, double pControlOffset )
		{
			const double lH = CanvasSize * 0.5;
			double lC = ClampControlOffset( pControlOffset );

			bool lIsTopLeft = ( pCorners & CornerSelection.TopLeft ) == CornerSelection.TopLeft;
			bool lIsTopRight = ( pCorners & CornerSelection.TopRight ) == CornerSelection.TopRight;
			bool lIsBottomRight = ( pCorners & CornerSelection.BottomRight ) == CornerSelection.BottomRight;
			bool lIsBottomLeft = ( pCorners & CornerSelection.BottomLeft ) == CornerSelection.BottomLeft;

			Point lLeftMid = new Point( 0.0, lH );
			Point lTopMid = new Point( lH, 0.0 );
			Point lRightMid = new Point( CanvasSize, lH );
			Point lBottomMid = new Point( lH, CanvasSize );

			StreamGeometry lGeometry = new StreamGeometry { FillRule = FillRule.Nonzero };

			using ( StreamGeometryContext lCtx = lGeometry.Open() )
			{
				lCtx.BeginFigure( lLeftMid, true, false );

				if ( lIsTopLeft )
				{
					lCtx.BezierTo( new Point( 0.0, lC ), new Point( lC, 0.0 ), lTopMid, true, true );
				}
				else
				{
					lCtx.LineTo( new Point( 0.0, 0.0 ), true, true );
					lCtx.LineTo( lTopMid, true, true );
				}

				if ( lIsTopRight )
				{
					lCtx.BezierTo( new Point( CanvasSize - lC, 0.0 ), new Point( CanvasSize, lC ), lRightMid, true, true );
				}
				else
				{
					lCtx.LineTo( new Point( CanvasSize, 0.0 ), true, true );
					lCtx.LineTo( lRightMid, true, true );
				}

				if ( lIsBottomRight )
				{
					lCtx.BezierTo( new Point( CanvasSize, CanvasSize - lC ), new Point( CanvasSize - lC, CanvasSize ), lBottomMid, true, true );
				}
				else
				{
					lCtx.LineTo( new Point( CanvasSize, CanvasSize ), true, true );
					lCtx.LineTo( lBottomMid, true, true );
				}

				if ( lIsBottomLeft )
				{
					lCtx.BezierTo( new Point( lC, CanvasSize ), new Point( 0.0, CanvasSize - lC ), lLeftMid, true, true );
				}
				else
				{
					lCtx.LineTo( new Point( 0.0, CanvasSize ), true, true );
					lCtx.LineTo( lLeftMid, true, true );
				}
			}

			return lGeometry;
		}

		public object Convert( object pInput, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			CornerSelection lCorners = ParseCorners( pInput );
			double lControlOffset = ParseControlOffset( pParameter );

			Geometry lGeometry = BuildGeometry( lCorners, lControlOffset );
			if ( lGeometry != null && lGeometry.CanFreeze )
			{
				lGeometry.Freeze();
			}

			return lGeometry ?? Geometry.Empty;
		}

		public object ConvertBack( object pInput, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			return Binding.DoNothing;
		}
	}

	public class TopOffsetConverter : IMultiValueConverter
	{
		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || pValues.Length < 2 || !( pValues[ 0 ] is Grid lParentGrid ) || !( pValues[ 1 ] is Path lCurrentPath ) )
			{
				return 0.0;
			}

			var lHighestPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.HighestPoint ).ToList();
			var lLowestPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.LowestPoint ).ToList();

			if ( lLowestPointsInGrid.Count <= 0 || lHighestPointsInGrid.Count <= 0 )
			{
				return 0.0;
			}

			var lHighestYInGrid = lHighestPointsInGrid.OrderBy( pPoint => pPoint.Y ).First();
			var lLowestYInGrid = lLowestPointsInGrid.OrderByDescending( pPoint => pPoint.Y ).First();

			double lTotalHeightOfPaths = lLowestYInGrid.Y - lHighestYInGrid.Y;

			var lHighestPointInCurrentPath = PathExtremesCalculatorHelper.HighestPoint( lCurrentPath );
			var lLowestPointInCurrentPath = PathExtremesCalculatorHelper.LowestPoint( lCurrentPath );
			double lHeightOfCurrentPath = lLowestPointInCurrentPath.Y - lHighestPointInCurrentPath.Y;

			double lOffsetYRelativeToLowest = ( lLowestYInGrid.Y - lLowestPointInCurrentPath.Y ) / lHeightOfCurrentPath;

			return 1 - ( lTotalHeightOfPaths / lHeightOfCurrentPath - lOffsetYRelativeToLowest );
		}

		public object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	public class BottomOffsetConverter : IMultiValueConverter
	{
		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || pValues.Length < 2 || !( pValues[ 0 ] is Grid lParentGrid ) || !( pValues[ 1 ] is Path lCurrentPath ) )
			{
				return 1.0;
			}

			var lLowestPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.LowestPoint ).ToList();

			if ( lLowestPointsInGrid.Count <= 0 )
			{
				return 1.0;
			}

			var lLowestYInGrid = lLowestPointsInGrid.OrderByDescending( pPoint => pPoint.Y ).First();

			var lHighestPointInCurrentPath = PathExtremesCalculatorHelper.HighestPoint( lCurrentPath );
			var lLowestPointInCurrentPath = PathExtremesCalculatorHelper.LowestPoint( lCurrentPath );
			double lHeightOfCurrentPath = lLowestPointInCurrentPath.Y - lHighestPointInCurrentPath.Y;

			double lOffsetYRelativeToLowest = ( lLowestYInGrid.Y - lLowestPointInCurrentPath.Y ) / lHeightOfCurrentPath;

			return lOffsetYRelativeToLowest + 1;
		}

		public object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	public class LeftOffsetConverter : IMultiValueConverter
	{
		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || pValues.Length < 2 || !( pValues[ 0 ] is Grid lParentGrid ) || !( pValues[ 1 ] is Path lCurrentPath ) )
			{
				return 0.0;
			}

			var lLeftmostPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.LeftmostPoint ).ToList();
			var lRightmostPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.RightmostPoint ).ToList();

			if ( lLeftmostPointsInGrid.Count <= 0 || lRightmostPointsInGrid.Count <= 0 )
			{
				return 0.0;
			}

			var lLeftmostXInGrid = lLeftmostPointsInGrid.OrderBy( pPoint => pPoint.X ).First();
			var lRightmostXInGrid = lRightmostPointsInGrid.OrderByDescending( pPoint => pPoint.X ).First();

			double lTotalHeightOfPaths = lRightmostXInGrid.X - lLeftmostXInGrid.X;

			var lLeftmostPointInCurrentPath = PathExtremesCalculatorHelper.LeftmostPoint( lCurrentPath );
			var lRightmostPointInCurrentPath = PathExtremesCalculatorHelper.RightmostPoint( lCurrentPath );
			double lHeightOfCurrentPath = lRightmostPointInCurrentPath.X - lLeftmostPointInCurrentPath.X;

			double lOffsetXRelativeToRightmost = ( lRightmostXInGrid.X - lRightmostPointInCurrentPath.X ) / lHeightOfCurrentPath;

			return 1 - ( lTotalHeightOfPaths / lHeightOfCurrentPath - lOffsetXRelativeToRightmost );
		}

		public object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	public class RightOffsetConverter : IMultiValueConverter
	{
		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || pValues.Length < 2 || !( pValues[ 0 ] is Grid lParentGrid ) || !( pValues[ 1 ] is Path lCurrentPath ) )
			{
				return 1.0;
			}

			var lRightmostPointsInGrid = lParentGrid.Children.OfType<Path>().Select( PathExtremesCalculatorHelper.RightmostPoint ).ToList();

			if ( lRightmostPointsInGrid.Count <= 0 )
			{
				return 1.0;
			}

			var lRightmostXInGrid = lRightmostPointsInGrid.OrderByDescending( pPoint => pPoint.X ).First();

			var lLeftmostPointInCurrentPath = PathExtremesCalculatorHelper.LeftmostPoint( lCurrentPath );
			var lRightmostPointInCurrentPath = PathExtremesCalculatorHelper.RightmostPoint( lCurrentPath );
			double lHeightOfCurrentPath = lRightmostPointInCurrentPath.X - lLeftmostPointInCurrentPath.X;

			double lOffsetXRelativeToRightmost = ( lRightmostXInGrid.X - lRightmostPointInCurrentPath.X ) / lHeightOfCurrentPath;

			return lOffsetXRelativeToRightmost + 1;
		}

		public object[] ConvertBack( object pValue, Type[] pTargetUiTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	[ValueConversion( typeof( object[] ), typeof( object ) )]
	public class MathOperationConverter : IMultiValueConverter
	{
		private static readonly ThicknessConverter sThicknessConverter = new ThicknessConverter();

		private static object[] GetCleanValues( IEnumerable<object> pValues )
		{
			return pValues
				.Where( pEntry => pEntry != null && !ReferenceEquals( pEntry, DependencyProperty.UnsetValue ) && !ReferenceEquals( pEntry, Binding.DoNothing ) )
				.ToArray();
		}

		private static bool TryParseParameter( object pParameter, out MathOperation pOperation, out bool pIsInverted )
		{
			string lRaw = pParameter.ToString();
			string lNormalized = lRaw.Trim().ToLowerInvariant();

			string[] lTokens = lNormalized
				.Split( new[] { ' ', ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries )
				.ToArray();

			pIsInverted = lTokens.Any( IsInvertToken );

			foreach ( string lToken in lTokens )
			{
				if ( TryParseOperationToken( lToken, out pOperation ) )
				{
					return true;
				}
			}

			pOperation = MathOperation.Addition;
			return false;
		}

		private static bool IsInvertToken( string pToken )
		{
			switch ( pToken )
			{
				case "inverse":
				case "invert":
				case "inversey":
				case "revers":
				case "reverse":
				case "negate":
				case "opposite":
					{
						return true;
					}
				default:
					{
						return false;
					}
			}
		}

		private static bool TryParseOperationToken( string pToken, out MathOperation pOperation )
		{
			switch ( pToken )
			{
				case "add":
				case "plus":
				case "addition":
					{
						pOperation = MathOperation.Addition;
						return true;
					}
				case "sub":
				case "subtract":
				case "minus":
				case "subtraction":
					{
						pOperation = MathOperation.Subtraction;
						return true;
					}
				case "mul":
				case "times":
				case "multiply":
				case "multiplication":
					{
						pOperation = MathOperation.Multiply;
						return true;
					}
				case "div":
				case "divide":
				case "division":
					{
						pOperation = MathOperation.Division;
						return true;
					}
				case "mod":
				case "remainder":
				case "modulo":
					{
						pOperation = MathOperation.Modulo;
						return true;
					}
				default:
					{
						pOperation = MathOperation.Addition;
						return false;
					}
			}
		}

		private static object ConvertUnaryInvert( object pValue, IFormatProvider pCulture )
		{
			if ( TryGetDouble( pValue, pCulture, out var lDoubleValue ) )
			{
				return -lDoubleValue;
			}

			return TryGetThickness( pValue, out var lThicknessValue ) ? NegateThickness( lThicknessValue ) : Binding.DoNothing;
		}

		private static object AccumulateFromFirstDouble( double pStart, IReadOnlyList<object> pValues, MathOperation pOperation, bool pIsInverted, CultureInfo pCulture )
		{
			double lAccumulator = pStart;

			for ( int lCurrentIndex = 1; lCurrentIndex < pValues.Count; lCurrentIndex++ )
			{
				if ( !TryGetPreparedScalar( pValues[ lCurrentIndex ], pCulture, pIsInverted, out var lPreparedScalar ) )
				{
					return Binding.DoNothing;
				}

				lAccumulator = ApplyBinaryOperation( lAccumulator, lPreparedScalar, pOperation );
			}

			return lAccumulator;
		}

		private static object AccumulateFromFirstThickness( Thickness pStart, object[] pValues, MathOperation pOperation, bool pIsInverted, CultureInfo pCulture )
		{
			Thickness lAccumulator = pStart;

			for ( int lCurrentIndex = 1; lCurrentIndex < pValues.Length; lCurrentIndex++ )
			{
				if ( TryGetPreparedScalar( pValues[ lCurrentIndex ], pCulture, pIsInverted, out var lPreparedScalar ) )
				{
					lAccumulator = ApplyOperation( lAccumulator, lPreparedScalar, pOperation );
					continue;
				}

				if ( !TryGetPreparedThickness( pValues[ lCurrentIndex ], pIsInverted, out var lPreparedThickness ) )
				{
					return Binding.DoNothing;
				}

				lAccumulator = ApplyOperation( lAccumulator, lPreparedThickness, pOperation );
			}

			return lAccumulator;
		}

		private static bool TryGetPreparedScalar( object pInput, IFormatProvider pCulture, bool pIsInverted, out double pScalar )
		{
			if ( TryGetDouble( pInput, pCulture, out double lDoubleValue ) )
			{
				pScalar = pIsInverted ? -lDoubleValue : lDoubleValue;
				return true;
			}

			if ( TryGetThickness( pInput, out Thickness lThicknessValue ) )
			{
				double lAverage = AverageThickness( lThicknessValue );
				pScalar = pIsInverted ? -lAverage : lAverage;
				return true;
			}

			pScalar = 0.0;
			return false;
		}

		private static bool TryGetPreparedThickness( object pInput, bool pIsInverted, out Thickness pThickness )
		{
			if ( TryGetThickness( pInput, out Thickness lThickness ) )
			{
				pThickness = pIsInverted ? NegateThickness( lThickness ) : lThickness;
				return true;
			}

			pThickness = new Thickness( 0 );
			return false;
		}

		private static bool TryGetDouble( object pInput, IFormatProvider pCulture, out double pResult )
		{
			switch ( pInput )
			{
				case double lDoubleValue:
					{
						pResult = lDoubleValue;
						return true;
					}
				case string lStringValue:
					{
						bool lHasParsed = double.TryParse( lStringValue, NumberStyles.Any, pCulture, out double lParsedValue );
						if ( lHasParsed )
						{
							pResult = lParsedValue;
							return true;
						}
						pResult = 0.0;
						return false;
					}
			}

			if ( pInput is IConvertible && !( pInput is bool ) && !( pInput is char ) )
			{
				try
				{
					pResult = System.Convert.ToDouble( pInput, pCulture );
					return true;
				}
				catch ( Exception )
				{
					// ignored
				}
			}

			pResult = 0.0;
			return false;
		}

		private static bool TryGetThickness( object pInput, out Thickness pResult )
		{
			switch ( pInput )
			{
				case Thickness lThickness:
					{
						pResult = lThickness;
						return true;
					}
				case string lString:
					{
						try
						{
							object lConverted = sThicknessConverter.ConvertFromString( lString );
							if ( lConverted is Thickness lParsed )
							{
								pResult = lParsed;
								return true;
							}
						}
						catch ( Exception )
						{
							// ignored
						}

						break;
					}
			}

			pResult = new Thickness( 0 );
			return false;
		}

		private static Thickness NegateThickness( Thickness pThickness )
		{
			return new Thickness( -pThickness.Left, -pThickness.Top, -pThickness.Right, -pThickness.Bottom );
		}

		private static double AverageThickness( Thickness pThickness )
		{
			return ( pThickness.Left + pThickness.Top + pThickness.Right + pThickness.Bottom ) / 4.0;
		}

		private static double ApplyBinaryOperation( double pLeft, double pRight, MathOperation pOperation )
		{
			switch ( pOperation )
			{
				case MathOperation.Addition:
					{
						return pLeft + pRight;
					}
				case MathOperation.Subtraction:
					{
						return pLeft - pRight;
					}
				case MathOperation.Multiply:
					{
						return pLeft * pRight;
					}
				case MathOperation.Division:
					{
						return Math.Abs( pRight ) > double.Epsilon ? pLeft / pRight : 0.0;
					}
				case MathOperation.Modulo:
					{
						return Math.Abs( pRight ) > double.Epsilon ? pLeft % pRight : 0.0;
					}
				default:
					{
						return 0.0;
					}
			}
		}

		private static Thickness ApplyOperation( Thickness pLeft, double pRight, MathOperation pOperation )
		{
			return new Thickness
			(
				ApplyBinaryOperation( pLeft.Left, pRight, pOperation ),
				ApplyBinaryOperation( pLeft.Top, pRight, pOperation ),
				ApplyBinaryOperation( pLeft.Right, pRight, pOperation ),
				ApplyBinaryOperation( pLeft.Bottom, pRight, pOperation )
			);
		}

		private static Thickness ApplyOperation( Thickness pLeft, Thickness pRight, MathOperation pOperation )
		{
			return new Thickness
			(
				ApplyBinaryOperation( pLeft.Left, pRight.Left, pOperation ),
				ApplyBinaryOperation( pLeft.Top, pRight.Top, pOperation ),
				ApplyBinaryOperation( pLeft.Right, pRight.Right, pOperation ),
				ApplyBinaryOperation( pLeft.Bottom, pRight.Bottom, pOperation )
			);
		}

		public object Convert( object[] pValues, Type pTargetType, object pParameter, CultureInfo pCulture )
		{
			if ( pValues == null || pValues.Length == 0 || pParameter == null )
			{
				return Binding.DoNothing;
			}

			object[] lCleanValues = GetCleanValues( pValues );
			if ( lCleanValues.Length == 0 )
			{
				return Binding.DoNothing;
			}

			bool lHasOperation = TryParseParameter( pParameter, out var lOperation, out var lIsInverted );

			switch ( lHasOperation )
			{
				case false when lCleanValues.Length == 1:
					{
						return ConvertUnaryInvert( lCleanValues[ 0 ], pCulture );
					}
				case false:
					{
						return Binding.DoNothing;
					}
			}

			object lFirst = lCleanValues[ 0 ];

			if ( TryGetDouble( lFirst, pCulture, out var lCurrentDoubleValue ) )
			{
				return AccumulateFromFirstDouble( lCurrentDoubleValue, lCleanValues, lOperation, lIsInverted, pCulture );
			}

			return TryGetThickness( lFirst, out var lCurrentThicknessValue ) ? AccumulateFromFirstThickness( lCurrentThicknessValue, lCleanValues, lOperation, lIsInverted, pCulture ) : Binding.DoNothing;
		}

		public object[] ConvertBack( object pValue, Type[] pTargetTypes, object pParameter, CultureInfo pCulture )
		{
			throw new NotImplementedException();
		}
	}

	public enum MathOperation
	{
		Addition,
		Subtraction,
		Multiply,
		Division,
		Modulo
	}
}
