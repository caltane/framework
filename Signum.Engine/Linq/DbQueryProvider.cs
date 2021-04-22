using System;
using System.Linq;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using System.Threading.Tasks;
using System.Threading;

namespace Signum.Engine.Linq
{

    /// <summary>
    /// Stateless query provider
    /// </summary>
    public class DbQueryProvider : QueryProvider, IQueryProviderAsync
    {
        public static readonly DbQueryProvider Single = new DbQueryProvider();

        private DbQueryProvider()
        {
        }

        public override string GetQueryText(Expression expression)
        {
            return this.Translate(expression, tr => tr.CleanCommandText());
        }

        public SqlPreCommandSimple GetMainSqlCommand(Expression expression)
        {
            return this.Translate(expression, tr => tr.MainCommand);
        }

        public override object? Execute(Expression expression)
        {
            using (HeavyProfiler.Log("DBQuery", () => expression.Type.TypeName()))
                return this.Translate(expression, tr => tr.Execute()!);
        }

        public async Task<object?> ExecuteAsync(Expression expression, CancellationToken token)
        {
            using (HeavyProfiler.Log("DBQuery", () => expression.Type.TypeName()))
                return await this.Translate(expression, tr => tr.ExecuteAsync(token));
        }

        public ITranslateResult GetRawTranslateResult(Expression expression)
        {
            return Translate(expression, tr => tr);
        }

        internal protected virtual R Translate<R>(Expression expression, Func<ITranslateResult, R> continuation) //For debugging purposes
        {
            AliasGenerator aliasGenerator = new AliasGenerator();

            ITranslateResult result;

            using (HeavyProfiler.Log("LINQ", () => expression.ToString()))
            using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
            {
                Expression cleaned = Clean(expression, true, log)!;
                var binder = new QueryBinder(aliasGenerator);
                log.Switch("Bind");
                ProjectionExpression binded = (ProjectionExpression)binder.BindQuery(cleaned);
                ProjectionExpression optimized = (ProjectionExpression)Optimize(binded, binder,aliasGenerator, log);
                log.Switch("ChPrjFlatt");
                ProjectionExpression flat = ChildProjectionFlattener.Flatten(optimized, aliasGenerator);
                log.Switch("TB");
                result = TranslatorBuilder.Build(flat);
            }
            return continuation(result);

        }

        public static Expression? Clean(Expression? expression, bool filter, HeavyProfiler.Tracer? log)
        {
            Expression? clean = ExpressionCleaner.Clean(expression);
            log.Switch("OvrLdSmp");
            Expression? simplified = OverloadingSimplifier.Simplify(clean);
            log.Switch("QrFlr");
            Expression? filtered = QueryFilterer.Filter(simplified, filter);
            return filtered;
        }

        internal static Expression Optimize(Expression binded, QueryBinder binder, AliasGenerator aliasGenerator, HeavyProfiler.Tracer? log)
        {
            var isPostgres = Schema.Current.Settings.IsPostgres;


            log.Switch("Aggregate");
            Expression rewriten = AggregateRewriter.Rewrite(binded);
            log.Switch("DupHistory");
            Expression dupHistory = DuplicateHistory.Rewrite(rewriten, aliasGenerator);
            log.Switch("EntityCompleter");
            Expression completed = EntityCompleter.Complete(dupHistory, binder);
            log.Switch("AliasReplacer");
            Expression replaced = AliasProjectionReplacer.Replace(completed, aliasGenerator);
            log.Switch("OrderBy");
            Expression orderRewrited = OrderByRewriter.Rewrite(replaced);
            log.Switch("Rebinder");
            Expression rebinded = QueryRebinder.Rebind(orderRewrited);
            log.Switch("UnusedColumn");
            Expression columnCleaned = UnusedColumnRemover.Remove(rebinded);
            log.Switch("Redundant");
            Expression subqueryCleaned = RedundantSubqueryRemover.Remove(columnCleaned);
            log.Switch("Condition");
            Expression rewriteConditions = isPostgres ? ConditionsRewriterPostgres.Rewrite(subqueryCleaned) : ConditionsRewriter.Rewrite(subqueryCleaned);
            log.Switch("Scalar");
            Expression scalar = ScalarSubqueryRewriter.Rewrite(rewriteConditions);
            return scalar;
        }

        internal protected virtual R Delete<R>(IQueryable query, Func<SqlPreCommandSimple, R> continuation, bool removeSelectRowCount = false)
        {
            AliasGenerator aliasGenerator = new AliasGenerator();

            SqlPreCommandSimple cr;
            using (HeavyProfiler.Log("LINQ"))
            using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
            {
                Expression cleaned = Clean(query.Expression, true, log)!;

                log.Switch("Bind");
                var binder = new QueryBinder(aliasGenerator);
                CommandExpression delete = binder.BindDelete(cleaned);
                CommandExpression deleteOptimized = (CommandExpression)Optimize(delete, binder, aliasGenerator, log);
                CommandExpression deleteSimplified = CommandSimplifier.Simplify(deleteOptimized, removeSelectRowCount, aliasGenerator);

                cr = TranslatorBuilder.BuildCommandResult(deleteSimplified);
            }
            return continuation(cr);
        }

        internal protected virtual R Update<R>(IUpdateable updateable, Func<SqlPreCommandSimple, R> continuation, bool removeSelectRowCount = false)
        {
            AliasGenerator aliasGenerator = new AliasGenerator();

            SqlPreCommandSimple cr;
            using (HeavyProfiler.Log("LINQ"))
            using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
            {
                Expression cleaned = Clean(updateable.Query.Expression, true, log)!;

                var binder = new QueryBinder(aliasGenerator);
                log.Switch("Bind");
                CommandExpression update = binder.BindUpdate(cleaned, updateable.PartSelector,  updateable.SetterExpressions );
                CommandExpression updateOptimized = (CommandExpression)Optimize(update, binder, aliasGenerator, log);
                CommandExpression updateSimplified = CommandSimplifier.Simplify(updateOptimized, removeSelectRowCount, aliasGenerator);
                log.Switch("TR");
                cr = TranslatorBuilder.BuildCommandResult(updateSimplified);
            }
            return continuation(cr);
        }

        internal protected virtual R Insert<R>(IQueryable query, LambdaExpression constructor, ITable table, Func<SqlPreCommandSimple, R> continuation, bool removeSelectRowCount = false)
        {
            AliasGenerator aliasGenerator = new AliasGenerator();

            SqlPreCommandSimple cr;
            using (HeavyProfiler.Log("LINQ"))
            using (var log = HeavyProfiler.LogNoStackTrace("Clean"))
            {
                Expression cleaned = Clean(query.Expression, true, log)!;
                var binder = new QueryBinder(aliasGenerator);
                log.Switch("Bind");
                CommandExpression insert = binder.BindInsert(cleaned, constructor, table);
                CommandExpression insertOprimized = (CommandExpression)Optimize(insert, binder, aliasGenerator, log);
                CommandExpression insertSimplified = CommandSimplifier.Simplify(insertOprimized, removeSelectRowCount, aliasGenerator);
                log.Switch("TR");
                cr = TranslatorBuilder.BuildCommandResult(insertSimplified);
            }
            return continuation(cr);
        }
    }


}
