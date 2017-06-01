using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;

namespace Syntactik.MonoDevelop.Parser
{
    public class SyntactikParsedDocument : DefaultParsedDocument
    {
        public SyntactikParsedDocument(string fileName) : base(fileName)
        {
            Foldings = new List<FoldingRegion>();
        }

        internal List<FoldingRegion> Foldings { get; private set; }
        readonly SemaphoreSlim foldingsSemaphore = new SemaphoreSlim(1, 1);

        public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Foldings == null)
            {
                return Task.Run(async delegate {
                    bool locked = false;
                    try
                    {
                        locked = await foldingsSemaphore.WaitAsync(Timeout.Infinite, cancellationToken);
                        if (Foldings == null)
                            Foldings = (await GenerateFoldings(cancellationToken)).ToList();
                    }
                    finally
                    {
                        if (locked)
                            foldingsSemaphore.Release();
                    }
                    return Foldings as IReadOnlyList<FoldingRegion>;
                }, cancellationToken);
            }
            return Task.FromResult(Foldings as IReadOnlyList<FoldingRegion>);
        }

        async Task<IEnumerable<FoldingRegion>> GenerateFoldings(CancellationToken cancellationToken)
        {
            return GenerateFoldingsInternal(await GetCommentsAsync(cancellationToken), cancellationToken);
        }

        IEnumerable<FoldingRegion> GenerateFoldingsInternal(IReadOnlyList<Comment> comments, CancellationToken cancellationToken)
        {
            foreach (var fold in comments.ToFolds())
                yield return fold;

            foreach (var fold in Foldings)
                yield return fold;




            //var visitor = new FoldingVisitor(cancellationToken);
            //if (Unit != null)
            //{
            //    try
            //    {
            //        visitor.Visit(Unit.GetRoot(cancellationToken));
            //    }
            //    catch (Exception) { }
            //}
            //foreach (var fold in visitor.Foldings)
            //    yield return fold;
        }
    }
}
