using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using Syntactik.Compiler;

namespace Syntactik.MonoDevelop.Parser
{
    public class SyntactikParsedDocument : ParsedDocument
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

        public void Add(CompilerError error)
        {
            Add(new Error(ErrorType.Error, error.Code, error.Message, error.LexicalInfo.Line, error.LexicalInfo.Column));
        }

        #region ParseDocument abstracts implementation

        public List<Error> Errors = new List<Error>();

        public void Add(Error error)
        {
            Errors.Add(error);
        }

        public void AddRange(IEnumerable<Error> errors)
        {
            Errors.AddRange(errors);
        }

        public override Task<IReadOnlyList<Error>> GetErrorsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IReadOnlyList<Error>>(Errors);
        }


        readonly List<Comment> _comments = new List<Comment>();

        public void Add(Comment comment)
        {
            _comments.Add(comment);
        }

        public void AddRange(IEnumerable<Comment> comments)
        {
            _comments.AddRange(comments);
        }

        public override Task<IReadOnlyList<Comment>> GetCommentsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IReadOnlyList<Comment>>(_comments);
        }

        readonly List<Tag> _tagComments = new List<Tag>();

        public void Add(Tag tagComment)
        {
            _tagComments.Add(tagComment);
        }

        public void AddRange(IEnumerable<Tag> tagComments)
        {
            this._tagComments.AddRange(tagComments);
        }

        public override Task<IReadOnlyList<Tag>> GetTagCommentsAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult<IReadOnlyList<Tag>>(_tagComments);
        }


        #endregion
    }
}
