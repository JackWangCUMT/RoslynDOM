using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using RoslynDom.Common;
using System.ComponentModel.DataAnnotations;
using System;
using System.Linq;

namespace RoslynDom
{
   /// <summary>
   /// 
   /// </summary>
   /// <remarks>
   /// Currently no constructor for making regions out of thin air because I haven't worked out
   /// how to match up start and end constructs. Probably a special method is needed
   /// <para>
   /// The RegionEnd property is filled when the RegionEnd is created.
   /// </para>
   /// </remarks>
   public class RDomDetailBlockStart : RDomDetail<IDetailBlockStart>, IDetailBlockStart
   {

      public RDomDetailBlockStart(IDom parent, SyntaxTrivia trivia, string text, SyntaxNode structuredNode)
          : base(parent, StemMemberKind.RegionStart, MemberKind.RegionStart, trivia, structuredNode)
      {
         _text = text;
         _groupGuid = Guid.NewGuid();
      }

      internal RDomDetailBlockStart(RDomDetailBlockStart oldRDom)
          : base(oldRDom)
      {
         _text = oldRDom.Text;
         // Assume we are in the midst of a copy and that both ends will be copied
         _oldGroupGuid = oldRDom.GroupGuid;
         _groupGuid = Guid.NewGuid();
      }

      public IDetailBlockEnd BlockEnd
      { get { return FindBlockEnd(); } }

      private IDetailBlockEnd FindBlockEnd()
      {
         // As a possibly premature optimization, check the parent container first as 
         // semantically correct nesting will have it there
         var container = this.Parent as IRDomContainer;
         if (container != null)
         {
            var fromParent = container.GetMembers()
                      .OfType<IDetailBlockEnd>()
                      .Where(x => x.GroupGuid == this.GroupGuid)
                      .FirstOrDefault();
            if (fromParent != null) { return fromParent; }
         }
         var rootOrBase = Ancestors.Last(); // Root
         var descendants = rootOrBase.Descendants
                      .OfType<IDetailBlockEnd>()
                      .Where(x => x.GroupGuid == this.GroupGuid)
                      .FirstOrDefault();
         if (descendants != null) { return descendants; }
         throw new InvalidOperationException("Matching end region not found");
      }

      public string BlockStyleName
      { get { return "region"; } }

      public IEnumerable<IDom> BlockContents
      {
         get
         {
            if (BlockEnd.Parent != Parent) return null;

            var parentContainer = Ancestors
                                    .OfType<IRDomContainer>()
                                    .FirstOrDefault();
            if (parentContainer == null) return null;

            var ret = parentContainer.GetMembers()
                        .FollowingSiblings(this);
            ret = ret.PreviousSiblings(BlockEnd);
            return ret.ToList();
         }
      }

      private Guid _groupGuid;
      public Guid GroupGuid { get { return _groupGuid; } }

      private Guid _oldGroupGuid;
      internal Guid OldGroupGuid
      {
         get { return _oldGroupGuid; }
         set
         {
            if (value != Guid.Empty) throw new InvalidOperationException("Can only reset old group guid to empty");
            _oldGroupGuid = value;
         }
      }

      private string _text;
      public string Text
      {
         get { return _text; }
         set { SetProperty(ref _text, value); }
      }

      public bool SemanticallyValid
      { get { return BlockEnd.Parent == this.Parent; } }
   }
}
