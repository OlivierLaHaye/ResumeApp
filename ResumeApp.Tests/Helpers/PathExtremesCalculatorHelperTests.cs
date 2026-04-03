using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using ResumeApp.Helpers;
using Xunit;

namespace ResumeApp.Tests.Helpers;

public sealed class PathExtremesCalculatorHelperTests
{
    private static Path CreatePathWithPoints( params Point[] pPoints )
    {
        var lFigure = new PathFigure { StartPoint = pPoints[0] };

        for ( int lIndex = 1; lIndex < pPoints.Length; lIndex++ )
        {
            lFigure.Segments.Add( new LineSegment( pPoints[lIndex], true ) );
        }

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );

        return new Path { Data = lGeometry };
    }

    [StaFact]
    public void HighestPoint_ReturnsPointWithSmallestY()
    {
        var lPath = CreatePathWithPoints(
            new Point( 0, 10 ),
            new Point( 5, 2 ),
            new Point( 10, 20 ) );

        var lResult = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( 2, lResult.Y );
        Assert.Equal( 5, lResult.X );
    }

    [StaFact]
    public void LowestPoint_ReturnsPointWithLargestY()
    {
        var lPath = CreatePathWithPoints(
            new Point( 0, 10 ),
            new Point( 5, 2 ),
            new Point( 10, 20 ) );

        var lResult = PathExtremesCalculatorHelper.LowestPoint( lPath );

        Assert.Equal( 20, lResult.Y );
    }

    [StaFact]
    public void LeftmostPoint_ReturnsPointWithSmallestX()
    {
        var lPath = CreatePathWithPoints(
            new Point( 10, 0 ),
            new Point( 2, 5 ),
            new Point( 20, 10 ) );

        var lResult = PathExtremesCalculatorHelper.LeftmostPoint( lPath );

        Assert.Equal( 2, lResult.X );
    }

    [StaFact]
    public void RightmostPoint_ReturnsPointWithLargestX()
    {
        var lPath = CreatePathWithPoints(
            new Point( 10, 0 ),
            new Point( 2, 5 ),
            new Point( 20, 10 ) );

        var lResult = PathExtremesCalculatorHelper.RightmostPoint( lPath );

        Assert.Equal( 20, lResult.X );
    }

    [StaFact]
    public void NullPathData_ReturnsDefaultPoint()
    {
        var lPath = new Path { Data = null };

        var lResult = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( default( Point ), lResult );
    }

    [StaFact]
    public void NullPath_ReturnsDefaultPoint()
    {
        var lPath = new Path();

        var lResult = PathExtremesCalculatorHelper.LowestPoint( lPath );

        Assert.Equal( default( Point ), lResult );
    }

    [StaFact]
    public void PathWithBezierSegment_ExtractsAllControlPoints()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new BezierSegment(
            new Point( 10, 50 ),
            new Point( 20, -10 ),
            new Point( 30, 30 ),
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -10, lHighest.Y );
    }

    [StaFact]
    public void PathWithPolyLineSegment_ExtractsAllPoints()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new PolyLineSegment(
            new PointCollection { new Point( 5, 10 ), new Point( 10, -5 ), new Point( 15, 20 ) },
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -5, lHighest.Y );
    }

    [StaFact]
    public void PathWithPolyBezierSegment_ExtractsAllPoints()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new PolyBezierSegment(
            new PointCollection { new Point( 1, -20 ), new Point( 2, 10 ), new Point( 3, 5 ) },
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -20, lHighest.Y );
    }

    [StaFact]
    public void PathWithQuadraticBezierSegment_ExtractsPoints()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new QuadraticBezierSegment(
            new Point( 5, -30 ),
            new Point( 10, 10 ),
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -30, lHighest.Y );
    }

    [StaFact]
    public void PathWithPolyQuadraticBezierSegment_ExtractsPoints()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new PolyQuadraticBezierSegment(
            new PointCollection { new Point( 1, -15 ), new Point( 2, 10 ) },
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -15, lHighest.Y );
    }

    [StaFact]
    public void PathWithArcSegment_ExtractsEndPoint()
    {
        var lFigure = new PathFigure { StartPoint = new Point( 0, 0 ) };
        lFigure.Segments.Add( new ArcSegment(
            new Point( 10, -25 ),
            new Size( 10, 10 ),
            0,
            false,
            SweepDirection.Clockwise,
            true ) );

        var lGeometry = new PathGeometry();
        lGeometry.Figures.Add( lFigure );
        var lPath = new Path { Data = lGeometry };

        var lHighest = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( -25, lHighest.Y );
    }

    [StaFact]
    public void PathWithNonPathGeometry_ConvertsGeometry()
    {
        var lPath = new Path { Data = new EllipseGeometry( new Point( 50, 50 ), 30, 30 ) };

        var lResult = PathExtremesCalculatorHelper.HighestPoint( lPath );

        // Should extract something without crashing
        Assert.IsType<Point>( lResult );
    }

    [StaFact]
    public void EmptyPathGeometry_ReturnsDefaultPoint()
    {
        var lPath = new Path { Data = new PathGeometry() };

        var lResult = PathExtremesCalculatorHelper.HighestPoint( lPath );

        Assert.Equal( default( Point ), lResult );
    }
}
