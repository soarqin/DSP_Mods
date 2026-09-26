using System;
using System.Collections.Generic;
using System.Threading;

namespace UXAssist.Production;

internal enum LinearSolveStatus
{
    Optimal,
    Infeasible,
    Unbounded,
    IterationLimit,
    Cancelled
}

internal sealed class LinearSolveResult
{
    internal LinearSolveStatus Status { get; }
    internal double[] Values { get; }

    internal LinearSolveResult(LinearSolveStatus status, double[] values = null)
    {
        Status = status;
        Values = values;
    }
}

internal static class TwoPhaseSimplex
{
    private const double PivotTolerance = 1e-10;
    private const double ReducedTolerance = 1e-8;

    internal static LinearSolveResult Solve(double[,] coefficients, double[] rightHandSide,
        IReadOnlyList<double[]> objectives, int iterationBudget, CancellationToken cancellation)
    {
        var rowCount = rightHandSide.Length;
        var columnCount = coefficients.GetLength(1);
        if (coefficients.GetLength(0) != rowCount || iterationBudget <= 0)
            throw new ArgumentException("The linear system or iteration budget is invalid.");
        foreach (var objective in objectives)
        {
            if (objective.Length != columnCount)
                throw new ArgumentException("An objective has an incorrect number of columns.");
        }

        if (rowCount == 0)
            return new LinearSolveResult(LinearSolveStatus.Optimal, new double[columnCount]);

        var width = columnCount + rowCount;
        var tableau = new double[rowCount, width + 1];
        var basis = new int[rowCount];
        for (var row = 0; row < rowCount; row++)
        {
            var scale = 0.0;
            for (var column = 0; column < columnCount; column++)
            {
                if (double.IsNaN(coefficients[row, column]) || double.IsInfinity(coefficients[row, column]))
                    throw new ArgumentException("The constraint matrix contains a non-finite number.");
                scale = Math.Max(scale, Math.Abs(coefficients[row, column]));
            }

            if (double.IsNaN(rightHandSide[row]) || double.IsInfinity(rightHandSide[row]))
                throw new ArgumentException("The right-hand side contains a non-finite number.");
            if (scale == 0)
            {
                if (Math.Abs(rightHandSide[row]) > PivotTolerance)
                    return new LinearSolveResult(LinearSolveStatus.Infeasible);
                scale = 1;
            }

            var sign = rightHandSide[row] < 0 ? -1 : 1;
            for (var column = 0; column < columnCount; column++)
                tableau[row, column] = sign * coefficients[row, column] / scale;
            tableau[row, columnCount + row] = 1;
            tableau[row, width] = sign * rightHandSide[row] / scale;
            basis[row] = columnCount + row;
        }

        var artificialObjective = new double[width];
        for (var row = 0; row < rowCount; row++) artificialObjective[columnCount + row] = 1;
        var iterations = 0;
        var firstStatus = Optimize(tableau, basis, artificialObjective, Array.Empty<double[]>(),
            columnCount, ref iterations, iterationBudget, cancellation);
        if (firstStatus != LinearSolveStatus.Optimal)
            return new LinearSolveResult(firstStatus == LinearSolveStatus.Unbounded
                ? LinearSolveStatus.Infeasible : firstStatus);

        for (var row = 0; row < rowCount; row++)
        {
            if (basis[row] < columnCount) continue;
            if (Math.Abs(tableau[row, width]) > ReducedTolerance)
                return new LinearSolveResult(LinearSolveStatus.Infeasible);

            for (var column = 0; column < columnCount; column++)
            {
                if (Contains(basis, column) || Math.Abs(tableau[row, column]) <= PivotTolerance) continue;
                Pivot(tableau, basis, row, column);
                break;
            }
        }

        var optimizedObjectives = new List<double[]>();
        foreach (var objective in objectives)
        {
            var costs = new double[width];
            Array.Copy(objective, costs, columnCount);
            var status = Optimize(tableau, basis, costs, optimizedObjectives, columnCount,
                ref iterations, iterationBudget, cancellation);
            if (status != LinearSolveStatus.Optimal) return new LinearSolveResult(status);
            optimizedObjectives.Add(costs);
        }

        var values = new double[columnCount];
        for (var row = 0; row < rowCount; row++)
        {
            if (basis[row] < columnCount)
                values[basis[row]] = Math.Max(0, tableau[row, width]);
        }

        return new LinearSolveResult(LinearSolveStatus.Optimal, values);
    }

    private static LinearSolveStatus Optimize(double[,] tableau, int[] basis, double[] costs,
        IReadOnlyList<double[]> previousCosts, int enteringLimit, ref int iterations, int budget,
        CancellationToken cancellation)
    {
        var rowCount = basis.Length;
        var rightColumn = tableau.GetLength(1) - 1;
        while (true)
        {
            if (cancellation.IsCancellationRequested) return LinearSolveStatus.Cancelled;
            var entering = -1;
            for (var column = 0; column < enteringLimit; column++)
            {
                if (Contains(basis, column) || ReducedCost(tableau, basis, costs, column) >= -ReducedTolerance)
                    continue;
                var preservesPrevious = true;
                foreach (var previous in previousCosts)
                {
                    if (Math.Abs(ReducedCost(tableau, basis, previous, column)) <= ReducedTolerance) continue;
                    preservesPrevious = false;
                    break;
                }

                if (preservesPrevious)
                {
                    entering = column;
                    break;
                }
            }

            if (entering < 0) return LinearSolveStatus.Optimal;
            if (iterations >= budget) return LinearSolveStatus.IterationLimit;

            var leaving = -1;
            var minRatio = double.PositiveInfinity;
            for (var row = 0; row < rowCount; row++)
            {
                var coefficient = tableau[row, entering];
                if (coefficient <= PivotTolerance) continue;
                var ratio = Math.Max(0, tableau[row, rightColumn]) / coefficient;
                if (ratio < minRatio - ReducedTolerance ||
                    Math.Abs(ratio - minRatio) <= ReducedTolerance &&
                    (leaving < 0 || basis[row] < basis[leaving]))
                {
                    minRatio = ratio;
                    leaving = row;
                }
            }

            if (leaving < 0) return LinearSolveStatus.Unbounded;
            Pivot(tableau, basis, leaving, entering);
            iterations++;
        }
    }

    private static double ReducedCost(double[,] tableau, int[] basis, double[] costs, int column)
    {
        var reduced = costs[column];
        for (var row = 0; row < basis.Length; row++)
            reduced -= costs[basis[row]] * tableau[row, column];
        return reduced;
    }

    private static bool Contains(int[] basis, int column)
    {
        foreach (var basic in basis)
        {
            if (basic == column) return true;
        }

        return false;
    }

    private static void Pivot(double[,] tableau, int[] basis, int pivotRow, int pivotColumn)
    {
        var rowCount = basis.Length;
        var width = tableau.GetLength(1);
        var divisor = tableau[pivotRow, pivotColumn];
        for (var column = 0; column < width; column++)
            tableau[pivotRow, column] /= divisor;

        for (var row = 0; row < rowCount; row++)
        {
            if (row == pivotRow) continue;
            var factor = tableau[row, pivotColumn];
            if (factor == 0) continue;
            for (var column = 0; column < width; column++)
                tableau[row, column] -= factor * tableau[pivotRow, column];
            tableau[row, pivotColumn] = 0;
        }

        tableau[pivotRow, pivotColumn] = 1;
        basis[pivotRow] = pivotColumn;
    }
}
