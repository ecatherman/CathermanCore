using System.Linq.Expressions;

namespace CathermanCore;

/// <summary>
/// Enables dynamic composition of LINQ expression trees for Entity Framework Core queries.
/// Allows combining filter expressions dynamically using AND / OR logical operators.
/// </summary>
public static class PredicateBuilder
{
    /// <summary>
    /// Creates a predicate expression that evaluates to <see langword="true"/> for all entities.
    /// Use this as the starting point when dynamically chaining conditions with <see cref="And{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of entity being filtered.</typeparam>
    /// <returns>An expression representing <c>f => true</c>.</returns>
    public static Expression<Func<T, bool>> True<T>() => f => true;

    /// <summary>
    /// Creates a predicate expression that evaluates to <see langword="false"/> for all entities.
    /// Use this as the starting point when dynamically chaining conditions with <see cref="Or{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of entity being filtered.</typeparam>
    /// <returns>An expression representing <c>f => false</c>.</returns>
    public static Expression<Func<T, bool>> False<T>()  => f => false;

    /// <summary>
    /// Combines two predicate expressions using a logical <c>OR</c> operation (<see cref="Expression.OrElse"/>).
    /// </summary>
    /// <typeparam name="T">The type of entity being filtered.</typeparam>
    /// <param name="expr1">The primary predicate expression.</param>
    /// <param name="expr2">The secondary predicate expression to join with <c>OR</c>.</param>
    /// <returns>A new expression representing <c>expr1 OR expr2</c>.</returns>
    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(
            Expression.OrElse(expr1.Body, invokedExpr),
            expr1.Parameters);
    }

    /// <summary>
    /// Combines two predicate expressions using a logical <c>AND</c> operation (<see cref="Expression.AndAlso"/>).
    /// </summary>
    /// <typeparam name="T">The type of entity being filtered.</typeparam>
    /// <param name="expr1">The primary predicate expression.</param>
    /// <param name="expr2">The secondary predicate expression to join with <c>AND</c>.</param>
    /// <returns>A new expression representing <c>expr1 AND expr2</c>.</returns>
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> expr1,
        Expression<Func<T, bool>> expr2)
    {
        var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
        return Expression.Lambda<Func<T, bool>>(
            Expression.AndAlso(expr1.Body, invokedExpr),
            expr1.Parameters);
    }
}
