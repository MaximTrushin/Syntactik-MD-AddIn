using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.Compiler;

namespace Syntactik.MonoDevelop.Parser
{
    public class SyntactikParsedDocument : DefaultParsedDocument
    {
        public SyntactikParsedDocument(string fileName, ITextSourceVersion contentVersion) : base(fileName)
        {
            ContentVersion = contentVersion;
            Foldings = new List<FoldingRegion>();
        }

        internal List<FoldingRegion> Foldings { get; private set; }

        public ITextSourceVersion ContentVersion { get; }

        readonly SemaphoreSlim foldingsSemaphore = new SemaphoreSlim(1, 1);

        public override Task<IReadOnlyList<FoldingRegion>> GetFoldingsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (Foldings == null)
            {
                return Task.Run(async delegate
                {
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

        public override Task<IReadOnlyList<Error>> GetErrorsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.GetErrorsAsync(cancellationToken);
        }

        IEnumerable<FoldingRegion> GenerateFoldingsInternal(IReadOnlyList<Comment> comments, CancellationToken cancellationToken)
        {
            foreach (var fold in comments.ToFolds())
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return fold;
            }


            foreach (var fold in Foldings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return fold;
            }
        }

        public void AddErrors(IEnumerable<CompilerError> contextErrors)
        {
            if (contextErrors != null)
                foreach (var error in contextErrors)
                {
                    Add(new Error(ErrorType.Error, error.Code, error.Message, error.LexicalInfo.Line, error.LexicalInfo.Column));
                }
        }
    }
}
